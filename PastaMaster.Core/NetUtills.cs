using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace PastaMaster.Core
{
    public static class NetUtills
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static async Task Init(IConfiguration config)
        {
        }

        public static async Task<JContainer> GetJsonUrl(string url)
        {
            using var client = new HttpClient();
            try
            {
                var context = await client.GetAsync(url);
                if (!context.IsSuccessStatusCode)
                {
                    Logger.Error($"Code for {url}: {context.StatusCode}");
                    return null;
                }

                var urlContents = await context.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JContainer>(urlContents);
            }
            catch (Exception e)
            {
                Logger.Error($"Error in net utills: {e}");
                return null;
            }
        }
    }
}