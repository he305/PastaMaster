using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PastaMaster.Core
{
    public class MessageRecord
    {
        [BsonId] 
        public ObjectId Id { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("message")]
        public string Message { get; set; }
        [BsonElement("date")]
        public DateTime DateTime { get; set; }
        [BsonElement("isPasta")]
        public bool IsPasta { get; set; }
        [BsonElement("pastaId")]
        public int PastaId { get; set; }
        [BsonElement("channel")] 
        public string Channel { get; set; }

        public MessageRecord(string name, string message, DateTime dateTime, bool isPasta, int pastaId, string channel)
        {
            Name = name;
            Message = message;
            DateTime = dateTime;
            IsPasta = isPasta;
            PastaId = pastaId;
            Channel = channel;
        }
    }
}