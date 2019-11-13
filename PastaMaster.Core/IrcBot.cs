using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace PastaMaster.Core
{
    public class IrcBot
    {
        private readonly TwitchClient _client;
        private readonly List<ChatMessage> _pastas = new List<ChatMessage>();
        private readonly List<ChatMessage> _cachedPastas = new List<ChatMessage>();
        private const int MinimumPastaSize = 50;
        private const int MinimumLevenshteinDistance = 80;
        private const int MinuteToAbortPasta = 30;
        private const int SaveInterval = 1;
        private readonly StreamerInfo _streamer;
        private static string _token;
        private readonly List<string> _emotes;
        private const string FfzUrl = "https://api.frankerfacez.com/v1/room/";
        private const string SubscriberEmotesUrl = "https://api.twitchemotes.com/api/v4/channels/";
        public bool IsRunning { get; set; }

        public static void InitCredentials(IConfiguration configuration)
        {
            _token = configuration["client_token"];
        }

        public IrcBot(StreamerInfo streamer)
        {
            _streamer = streamer;
            var credentials = new ConnectionCredentials("he305bot", _token);
            
            _client = new TwitchClient(protocol:ClientProtocol.TCP);
            _client.Initialize(credentials, streamer.Name);

            _client.OnLog += Client_OnLog;
            _client.OnJoinedChannel += Client_OnJoinedChannel;
            _client.OnMessageReceived += Client_OnMessageReceived;
            _client.OnWhisperReceived += Client_OnWhisperReceived;
            _client.OnNewSubscriber += Client_OnNewSubscriber;
            _client.OnConnected += Client_OnConnected;
            _client.OnDisconnected += Client_OnDisconnected;
            _client.OnError += Client_OnError;
            _client.OnConnectionError += Client_OnConnectionError;
            _client.OnMessageThrottled += Client_OnMessageThrottled;
            _emotes = new List<string>();
        }

        private static void Client_OnMessageThrottled(object sender, OnMessageThrottledEventArgs e)
        {
            Console.WriteLine($"THROTTLE: {e.Message}");
        }

        private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"CONNECTION ERROR: {e.Error.Message}");
        }

        private static void Client_OnError(object sender, OnErrorEventArgs e)
        {
            Console.WriteLine($"ERROR: {e.Exception.Message}");
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Console.WriteLine("DISCONNECTED");
            _client.Reconnect();
        }

        public async void RunClient()
        {
            //await GetFfzEmotes();
            //await GetSubscriberEmotes();

            IsRunning = true;
            var saveTask = new Task(CleanNonPastas);
            saveTask.Start();
            _client.Connect();
        }

        private async Task GetFfzEmotes()
        {
            if (await NetUtills.GetJsonUrl(FfzUrl + _streamer.Name.ToLower()) is JObject res)
            {
                var id = res["room"]["set"].ToString();
                foreach (var emote in res["sets"][id]["emoticons"])
                {
                    _emotes.Add(emote["name"].ToString());
                    Console.WriteLine(emote["name"].ToString());
                }
            }
        }

        private async Task GetSubscriberEmotes()
        {
            if (await NetUtills.GetJsonUrl(SubscriberEmotesUrl + _streamer.Id) is JObject res)
            {
                foreach (var emote in res["emotes"])
                {
                    _emotes.Add(emote["code"].ToString());
                    Console.WriteLine(emote["code"].ToString());
                }
            }
        }

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private static void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
        }

        private static void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
        }

        private async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            //Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");

            if (e.ChatMessage.Message.Length < MinimumPastaSize 
                || e.ChatMessage.Username.Contains("bot", StringComparison.InvariantCultureIgnoreCase)
                || e.ChatMessage.Username.Contains("streamlabs", StringComparison.InvariantCultureIgnoreCase)
                || !IsMessageValid(e.ChatMessage.Message)) return;
            
            var msg = new ChatMessage(e.ChatMessage.Username, e.ChatMessage.Message);
            foreach (var pasta in _pastas.Where(pasta => RegexUtills.GetLevenshteinDistancePercent(pasta.Message, msg.Message) >= MinimumLevenshteinDistance))
            {
                msg.IsPasta = true;
                pasta.IsPasta = true;
                Console.WriteLine($"FOUND PASTA: {msg.Message}");
                break;
            }
            _pastas.Add(msg);

            Console.WriteLine($"added {msg.Message}");
        }

        private static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Joined {e.Channel}");
        }

        private static void Client_OnLog(object sender, OnLogArgs e)
        {
        }

        private async void CleanNonPastas()
        {
            while (IsRunning)
            {
                await Task.Delay(SaveInterval * 60 * 1000);
                var listToCheck = new List<ChatMessage>(_pastas);
                for (var i = listToCheck.Count - 1; i >= 0; i--)
                {
                    var dateTime = DateTime.Now;
                    var span = dateTime - listToCheck[i].DateTime;
                    if (span.Seconds >= MinuteToAbortPasta && !listToCheck[i].IsPasta)
                        listToCheck.RemoveAt(i);
                }

                var listToSave = listToCheck.Where(pasta => pasta.IsPasta).ToList();

//                var pastasInDb = await MongoMessageRepository.GetAllPastas();
//                
//                var pastasToSaveToDb = new List<PastaRecord>();
//                foreach (var message in listToSave)
//                {
//                    var found = false;
//                    foreach (var pastaRecord in pastasInDb.Where(pastaRecord => message.PastaId == pastaRecord.PastaId || RegexUtills.GetLevenshteinDistancePercent(message.Message, pastaRecord.Message) >= 80))
//                    {
//                        found = true;
//                    }
//
//                    foreach (var pastaAlreadyInList in pastasToSaveToDb.Where(pastaAlreadyInList => message.PastaId == pastaAlreadyInList.PastaId || RegexUtills.GetLevenshteinDistancePercent(message.Message, pastaAlreadyInList.Message) >= 80))
//                    {
//                        found = true;
//                    }
//
//                    if (found) continue;
//                    var pastaToSave = new PastaRecord(message.Message, message.PastaId);
//                    pastasToSaveToDb.Add(pastaToSave);
//                }
//                
//                if (pastasToSaveToDb.Count > 0)
//                    await MongoMessageRepository.InsertPastas(pastasToSaveToDb);

                var nonIntersect = listToSave.Except(_cachedPastas).ToList();

                var listToDatabase = nonIntersect.Select(message => new MessageRecord(
                        message.Name, 
                        message.Message, 
                        message.DateTime,
                        _streamer.Name)).ToList();

                if (listToDatabase.Count > 0)
                {
                    await MongoRepository.InsertMessages(listToDatabase);
                    _cachedPastas.AddRange(listToSave);
                }

                var json = JsonConvert.SerializeObject(listToSave);
                await using (var file = new StreamWriter(AppContext.BaseDirectory + $"{_streamer.Name}.json", false))
                {
                    file.WriteLine(json);
                }

                Console.WriteLine("Should be saved");
            }
        }

        private bool IsMessageValid(string msg)
        {
            msg = msg.Replace($"@{_streamer.Name}", string.Empty);
            //Check if message contains only one word
            var words = msg.Split(' ');
            var dictRecs = words.GroupBy(q => q).ToDictionary(x => x.Key, x => x.Count());
            if (dictRecs.Count < 2)
                return false;
            
            return true;
        }

        public void AbortBot()
        {
            _client.Disconnect();
        }

        private class ChatMessage
        {
            public string Name { get; set; }
            public string Message { get; set; }
            public DateTime DateTime { get; set; }
            public bool IsPasta { get; set; }

            public ChatMessage(string name, string message)
            {
                this.Name = name;
                this.Message = message;
                this.DateTime = DateTime.Now;
                this.IsPasta = false;
            }
        }
    }
}