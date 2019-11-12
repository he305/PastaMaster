using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace PastaMaster.Core
{
    public enum Platform
    {
        Twitch,
        Goodgame
    }

    public static class StreamerFactory
    {
        public static StreamerInfo CreateStreamer(string name, string id, Platform platform)
        {
            return platform switch
                {
                Platform.Twitch => (StreamerInfo) new StreamerInfoTwitch(name, id),
                Platform.Goodgame => new StreamerInfoGoodGame(name, id),
                _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
                };
        }
    }

    public abstract class StreamerInfo
    {
        public readonly string Id;
        public readonly string Name;
        protected string Game;
        public bool IsStreaming;
        
        protected IrcBot Bot;
        protected Task ChatTask;
        protected Thread ChatThread;

        protected StreamerInfo(string name, string id)
        {
            Name = name;
            Id = id;
            IsStreaming = false;
            Game = "";
        }

        public void SetStreaming(bool isStreaming)
        {
            IsStreaming = isStreaming;
        }

        public abstract Task<bool> GetStreamingStatus();
        public abstract Task<string> InitAtStart();
        public abstract Task<string> ProcessStreamer();
        public abstract Task<string> GetViewerCount();
        public abstract Task<string> EndStreamProcess();
    }

    public sealed class StreamerInfoTwitch : StreamerInfo
    {
        private string _title;

        public StreamerInfoTwitch(string name, string id) : base(name, id)
        {
        }

        public override async Task<bool> GetStreamingStatus()
        {
            return await StreamerFeeder.Api.V5.Streams.BroadcasterOnlineAsync(Id);
        }

        public override async Task<string> InitAtStart()
        {
            var sb = new StringBuilder($"@everyone\n{Name} is online\n");

            var curTitle = await GetTitle();
            sb.Append($"Title: {curTitle}\n");
            _title = curTitle;

            await Task.Delay(1 * 1000);

            var curGame = await GetStreamGame();
            sb.Append($"Game: {curGame}\n");
            Game = curGame;
            
            Bot = new IrcBot(this);
            //ChatThread = new Thread(Bot.RunClient);
            //ChatThread.Start();

            ChatTask = new Task(Bot.RunClient);

            ChatTask.Start();
            
            return sb.ToString();
        }

        private void ExceptionHandler(Task task)
        {
            var exception = task.Exception;
            Console.WriteLine(exception);
        }

        public override async Task<string> ProcessStreamer()
        {
            var sb = new StringBuilder("");
            var curGame = await GetStreamGame();

            if (!curGame.Equals(Game))
            {
                Game = curGame;
                sb.Append($"{Name} changed game to {curGame}\n");
                await Task.Delay(1 * 1000);
            }

            var curTitle = await GetTitle();

            if (!curTitle.Equals(_title))
            {
                _title = curTitle;
                sb.Append($"{Name} changed title to {curTitle}\n");
                await Task.Delay(1 * 1000);
            }

            if (sb.Length <= 1) return sb.ToString();
            sb.Insert(0, "@everyone\n");
            var viewers = await GetViewerCount();
            sb.Append($"Viewers: {viewers}\n");

            return sb.ToString();
        }

        public override async Task<string> GetViewerCount()
        {
            if (!IsStreaming) return "offline";

            var res = await StreamerFeeder.Api.V5.Streams.GetStreamByUserAsync(Id);
            return res.Stream == null ? "0" : res.Stream.Viewers.ToString();
        }

        public override async Task<string> EndStreamProcess()
        {
            if (ChatThread.IsAlive)
            {
                Bot.IsRunning = false;
                Bot.AbortBot();
                ChatThread.Abort();   
            }
            
            
            var sb = new StringBuilder($"{Name} went offline\n");

            var curTitle = await GetTitle();
            sb.Append($"Title: {curTitle}\n");
            _title = curTitle;

            await Task.Delay(1 * 1000);

            var curGame = await GetStreamGame();
            sb.Append($"Game: {curGame}\n");
            Game = curGame;

            return sb.ToString();
        }

        private async Task<string> GetStreamGame()
        {
            var res = await StreamerFeeder.Api.V5.Streams.GetStreamByUserAsync(Id);
            return res.Stream.Game;
        }

        private async Task<string> GetTitle()
        {
            var res = await StreamerFeeder.Api.V5.Streams.GetStreamByUserAsync(Id);
            return res.Stream.Channel.Status;
        }
    }

    public sealed class StreamerInfoGoodGame : StreamerInfo
    {
        private const string GoodgameApi = "https://goodgame.ru/api/getchannelstatus?fmt=json";

        public StreamerInfoGoodGame(string name, string id) : base(name, id)
        {
        }

        public override async Task<bool> GetStreamingStatus()
        {
            if (!(await GetStreamDataGoodGame() is { } res)) return false;

            return !res[Id]["status"].ToString().Equals("Dead");
        }

        public override async Task<string> InitAtStart()
        {
            var sb = new StringBuilder($"@everyone\n{Name} is online\n");

            //EXCEPTIONS
            if (Name.Equals("gegeboyz")) return sb.ToString();

            var res = await GetStreamGame();
            sb.Append($"Game: {res}\n");
            Game = res;

            return sb.ToString();
        }

        public override async Task<string> GetViewerCount()
        {
            if (!IsStreaming) return "offline";

            return !(await GetStreamDataGoodGame() is { } res) ? "0" : res[Id]["viewers"].ToString();
        }

        public override async Task<string> EndStreamProcess()
        {
            var sb = new StringBuilder($"{Name} went offline\n");
            return sb.ToString();
        }

        private async Task<string> GetStreamGame()
        {
            return !(await GetStreamDataGoodGame() is { } res) ? "" : res[Id]["games"].ToString();
        }

        public override async Task<string> ProcessStreamer()
        {
            return "";
        }

        private async Task<JObject> GetStreamDataGoodGame()
        {
            var uriBuilder = new UriBuilder(GoodgameApi);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["id"] = Id;
            uriBuilder.Query = query.ToString();
            var url = uriBuilder.ToString();

            return await NetUtills.GetJsonUrl(url) as JObject;
        }
    }
}