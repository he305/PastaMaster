using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PastaMaster.Core
{
    public class MongoMessageRepository
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private static IMongoCollection<MessageRecord> _messagesCollection;

        public static void Init(IConfiguration config)
        {
            var clientSetting = MongoClientSettings.FromConnectionString(config["connection_string"]);
            _client = new MongoClient(clientSetting);
            /*var sett = new MongoClientSettings
            {
                Server = new MongoServerAddress(config["db_server"], int.Parse(config["db_port"])),
                Credential = MongoCredential.CreateCredential(config["db_name"], config["db_user"], config["db_pass"])
            };
            
            _client = new MongoClient(sett);*/
            
            _database = _client.GetDatabase(config["db_name"]);
            _messagesCollection = _database.GetCollection<MessageRecord>(config["messages"]);
        }

        public static async Task<bool> IsConnected()
        {
            return _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(3000);
        }

        public static async Task InsertMessage(MessageRecord msg)
        {
            await _messagesCollection.InsertOneAsync(msg);
        }

        public static async Task<List<MessageRecord>> GetAllMessages()
        {
            return await _messagesCollection.Find(new BsonDocument()).ToListAsync();
        }

        public static async Task InsertMessages(IEnumerable<MessageRecord> msgs)
        {
            await _messagesCollection.InsertManyAsync(msgs);
        }
    }
}