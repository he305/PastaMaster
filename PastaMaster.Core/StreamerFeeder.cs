using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using TwitchLib.Api;

namespace PastaMaster.Core
{
    public static class StreamerFeeder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static TwitchAPI Api;
        private static List<StreamerInfo> _streamers;
        private static bool _isRunning;
        private static IConfiguration _config;
        public static List<string> TwitchEmotes { get; set; }

        public static void Init(IConfiguration config)
        {
            _config = config;
            Api = new TwitchAPI();
            Api.Settings.ClientId = _config["client_id"];
            Api.Settings.AccessToken = _config["client_token"];

            _streamers = new List<StreamerInfo>();
            IrcBot.InitCredentials(_config);
        }

        public static async void RunStreamerFeeder()
        {
            if (!await PreStartMethod()) return;

            while (_isRunning)
            {
                foreach (var streamer in _streamers)
                {
                    var isStreaming = await streamer.GetStreamingStatus();

                    if (isStreaming && !streamer.IsStreaming)
                    {
                        streamer.SetStreaming(true);
                        var data = await streamer.InitAtStart();
                        Console.WriteLine(data);
                    }
                    else if (!isStreaming && streamer.IsStreaming)
                    {
                        streamer.SetStreaming(false);
                        var data = await streamer.EndStreamProcess();
                        Console.WriteLine(data);
                    }

                    await Task.Delay(1 * 1000);

                    if (!isStreaming) continue;
                    var processData = await streamer.ProcessStreamer();
                    Console.WriteLine(processData);
                }

                Logger.Info("Streamers have been checked");
                await Task.Run(DatabaseHandler.FixPastaIds);
                Logger.Info("Database optimized");
                await Task.Delay(60 * 1000);
            }
        }

        private static async Task<bool> PreStartMethod()
        {
            try
            {
                var streamers = JObject.Parse(File.ReadAllText("streamers.json"));
                foreach (var streamer in streamers["twitch"])
                {
                    string name;
                    string id;

                    if (string.IsNullOrEmpty(streamer["id"].ToString()))
                    {
                        var users = await Api.V5.Users.GetUserByNameAsync(streamer["name"].ToString());
                        name = users.Matches[0].Name;
                        id = users.Matches[0].Id;
                        Console.WriteLine(name + " " + id);
                        await Task.Delay(1 * 1000);
                    }
                    else
                    {
                        name = streamer["name"].ToString();
                        id = streamer["id"].ToString();
                    }

                    _streamers.Add(StreamerFactory.CreateStreamer(
                        name,
                        id,
                        Platform.Twitch));
                    Logger.Info(name);
                }

                foreach (var streamer in streamers["goodgame"])
                {
                    _streamers.Add(StreamerFactory.CreateStreamer(
                        streamer["name"].ToString(),
                        streamer["id"].ToString(),
                        Platform.Goodgame));
                    Logger.Info(streamer["name"].ToString());
                }
            }
            catch (Exception e)
            {
                Logger.Fatal($"Can't open streamers.json: {e}");
                return false;
            }
            
            _isRunning = true;
            return true;
        }

        public static async Task<string> GetViewerCount(string channelName)
        {
            foreach (var streamer in _streamers.Where(streamer => streamer.Name.Equals(channelName)))
                return await streamer.GetViewerCount();

            var users = await Api.V5.Users.GetUserByNameAsync(channelName);
            await Task.Delay(1 * 1000);
            if (users.Total == 0) return "No such channel found";
            var res = await Api.V5.Streams.GetStreamByUserAsync(users.Matches[0].Id);
            return res.Stream == null ? "offline" : res.Stream.Viewers.ToString();
        }
    }
}