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
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace PastaMaster.Core
{
    public class IrcBot
    {
        private readonly TwitchClient _client;
        private readonly List<ChatMessage> _pastas;
        private const int MinimumPastaSize = 30;
        private const int MinimumLevenshteinDistance = 80;
        private const int MinuteToAbortPasta = 2;
        private const int SaveInterval = 3;
        private static string _token;

        public static void InitCredentials(IConfiguration configuration)
        {
            _token = configuration["client_token"];
        }

        public IrcBot(StreamerInfo streamer)
        {
            var credentials = new ConnectionCredentials("he305bot", _token);
            
            _client = new TwitchClient();
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

        public void RunClient(object o)
        {
            Action cleanMethod = CleanNonPastas;
            var registry = new Registry();
            registry.Schedule(cleanMethod).ToRunEvery(SaveInterval).Minutes();
            JobManager.Initialize(registry);
            JobManager.JobException += info => Console.WriteLine("An error just happened with a scheduled job: " + info.Exception);

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
            Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");

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

        private void CleanNonPastas()
        {
            for (var i = _pastas.Count - 1; i >= 0; i--)
            {
                var dateTime = DateTime.Now;
                var span = dateTime - _pastas[i].DateTime;
                if (span.Minutes >= MinuteToAbortPasta && !_pastas[i].IsPasta)
                    _pastas.RemoveAt(i);
            }
            
            var json = JsonConvert.SerializeObject(_pastas);
            using (var file = new StreamWriter(AppContext.BaseDirectory + "test.json", false))
            {
                file.WriteLine(json);
            }
            Console.WriteLine("Should be saved");
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