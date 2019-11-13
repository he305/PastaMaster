using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PastaMaster.Core
{
    public class MongoRepository
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private static IMongoCollection<MessageRecord> _messagesCollection;
        private static IMongoCollection<PastaRecord> _pastasCollection;

        public static void Init(IConfiguration config)
        {
            var connectionString = config["connection_string"];
            //var clientSetting = MongoClientSettings.FromConnectionString(config["connection_string"]);
            _client = new MongoClient(connectionString);
            /*var sett = new MongoClientSettings
            {
                Server = new MongoServerAddress(config["db_server"], int.Parse(config["db_port"])),
                Credential = MongoCredential.CreateCredential(config["db_name"], config["db_user"], config["db_pass"])
            };
            
            _client = new MongoClient(sett);*/

            _database = _client.GetDatabase(config["db_name"]);
            _messagesCollection = _database.GetCollection<MessageRecord>(config["messages_collection"]);
            _pastasCollection = _database.GetCollection<PastaRecord>(config["pastas_collection"]);
        }

        public static bool IsConnected()
        {
            return _database.RunCommandAsync((Command<BsonDocument>) "{ping:1}").Wait(5000);
        }

        public static async Task InsertMessage(MessageRecord msg)
        {
            await _messagesCollection.InsertOneAsync(msg);
        }

        public static async Task InsertPasta(PastaRecord pasta)
        {
            await _pastasCollection.InsertOneAsync(pasta);
        }

        public static async Task<List<MessageRecord>> GetAllMessages()
        {
            return await _messagesCollection.Find(_ => true).ToListAsync();
        }

        public static async Task<List<PastaRecord>> GetAllPastas()
        {
            return await _pastasCollection.Find(_ => true).ToListAsync();
        }


        public static async Task InsertMessages(IEnumerable<MessageRecord> msgs)
        {
            await _messagesCollection.InsertManyAsync(msgs);
        }

        public static async Task InsertPastas(IEnumerable<PastaRecord> pastas)
        {
            await _pastasCollection.InsertManyAsync(pastas);
        }

        public static async Task<List<MessageRecord>> GetMessagesByField(string fieldName, string fieldValue)
        {
            var filter = Builders<MessageRecord>.Filter.Eq(fieldName, fieldValue);
            var result = await _messagesCollection.Find(filter).ToListAsync();

            return result;
        }

        public static async Task<List<PastaRecord>> GetPastasByField(string fieldName, string fieldValue)
        {
            var filter = Builders<PastaRecord>.Filter.Eq(fieldName, fieldValue);
            var result = await _pastasCollection.Find(filter).ToListAsync();

            return result;
        }

        public static async Task<MessageRecord> GetFirstMessageByField(string fieldName, string fieldValue)
        {
            var filter = Builders<MessageRecord>.Filter.Eq(fieldName, fieldValue);
            var result = await _messagesCollection.Find(filter).SingleAsync();

            return result;
        }

        public static async Task<bool> UpdateMessage(ObjectId id, string udateFieldName, string updateFieldValue)
        {
            var filter = Builders<MessageRecord>.Filter.Eq("_id", id);
            var update = Builders<MessageRecord>.Update.Set(udateFieldName, updateFieldValue);

            var result = await _messagesCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount != 0;
        }

        public static async Task<bool> DeleteMessageById(ObjectId id)
        {
            var filter = Builders<MessageRecord>.Filter.Eq("_id", id);
            var result = await _messagesCollection.DeleteOneAsync(filter);
            return result.DeletedCount != 0;
        }
    }
}