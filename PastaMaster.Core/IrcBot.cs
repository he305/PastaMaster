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
        private readonly List<ChatMessage> _pastas;
        private readonly List<ChatMessage> _cachedPastas;
        private const int MinimumPastaSize = 30;
        private const int MinimumLevenshteinDistance = 80;
        private const int MinuteToAbortPasta = 30;
        private const int SaveInterval = 1;
        private readonly StreamerInfo _streamer;
        private static string _token;

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
            _pastas = new List<ChatMessage>();
            _cachedPastas = new List<ChatMessage>();
        }

        private void Client_OnMessageThrottled(object sender, OnMessageThrottledEventArgs e)
        {
            Console.WriteLine($"THROTTLE: {e.Message}");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"CONNECTION ERROR: {e.Error.Message}");
        }

        private void Client_OnError(object sender, OnErrorEventArgs e)
        {
            Console.WriteLine($"ERROR: {e.Exception.Message}");
        }

        private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Console.WriteLine("DISCONNECTED");
        }

        public void RunClient()
        {
            //Action cleanMethod = CleanNonPastas;
            /*var registry = new Registry();
            registry.Schedule(cleanMethod).ToRunEvery(SaveInterval).Minutes();
            JobManager.Initialize(registry);
            JobManager.JobException += info => Console.WriteLine("An error just happened with a scheduled job: " + info.Exception);*/

            var saveTask = new Task(CleanNonPastas);
            saveTask.Start();
            _client.Connect();
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
        }

        private async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            //Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");

            if (e.ChatMessage.Message.Length < MinimumPastaSize 
                || e.ChatMessage.Username.Contains("bot", StringComparison.InvariantCultureIgnoreCase)
                || e.ChatMessage.Username.Contains("streamlabs", StringComparison.InvariantCultureIgnoreCase)) return;
            
            var msg = new ChatMessage(e.ChatMessage.Username, e.ChatMessage.Message);
            foreach (var pasta in _pastas.Where(pasta => RegexUtills.GetLevenshteinDistancePercent(pasta.Message, msg.Message) >= MinimumLevenshteinDistance))
            {
                msg.IsPasta = true;
                pasta.IsPasta = true;
                msg.PastaId = pasta.PastaId;
                Console.WriteLine($"FOUND PASTA: {msg.Message}");
                break;
            }
            _pastas.Add(msg);

            Console.WriteLine($"added {msg.Message}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Joined {e.Channel}");
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
        }

        private async void CleanNonPastas()
        {
            while (true)
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

                var listToSave = new List<ChatMessage>();

                foreach (var pasta in listToCheck)
                {
                    if (pasta.IsPasta)
                        listToSave.Add(pasta);
                }

                var nonIntersect = listToSave.Except(_cachedPastas);

                var listToDatabase = nonIntersect.Select(message => new MessageRecord(message.Name, message.Message, message.DateTime, message.IsPasta, message.PastaId, _streamer.Name)).ToList();

                await MongoMessageRepository.InsertMessages(listToDatabase);
                
                var json = JsonConvert.SerializeObject(listToSave);
                await using (var file = new StreamWriter(AppContext.BaseDirectory + $"{_streamer.Name}.json", false))
                {
                    file.WriteLine(json);
                }

                Console.WriteLine("Should be saved");
            }
        }

        private class ChatMessage
        {
            private static int id = 0;
            public string Name { get; set; }
            public string Message { get; set; }
            public DateTime DateTime { get; set; }
            public bool IsPasta { get; set; }
            public int PastaId { get; set; }

            public ChatMessage(string name, string message)
            {
                this.Name = name;
                this.Message = message;
                this.DateTime = DateTime.Now;
                this.IsPasta = false;
                this.PastaId = id++;
            }
        }
    }
}