using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace PastaMaster.Core
{
    class Program
    {
        private IConfiguration _config;
        private Program()
        {
            var config = new LoggingConfiguration();

            var logfile = new FileTarget("logfile") {FileName = "file.txt"};
            var logconsole = new ConsoleTarget("logconsole");

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json");
            _config = builder.Build();

        }
        private static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            MongoRepository.Init(_config);
            if (!MongoRepository.IsConnected())
            {
                Console.WriteLine("Database is not connected");
                return;
            }

            Console.WriteLine("Connected");
            
            await AddInfiniteTasks();
            await Task.Delay(Timeout.Infinite);
        }

        private Task AddInfiniteTasks()
        {
            StreamerFeeder.Init(_config);

            Task.Run(StreamerFeeder.RunStreamerFeeder);
            
            return Task.CompletedTask;
        }
        
        
    }
}