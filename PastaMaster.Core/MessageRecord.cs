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
        [BsonElement("channel")] 
        public string Channel { get; set; }

        public MessageRecord(string name, string message, DateTime dateTime, string channel)
        {
            Name = name;
            Message = message;
            DateTime = dateTime;
            Channel = channel;
        }
    }

    public class PastaRecord
    {
        [BsonId] 
        public ObjectId Id { get; set; }
        [BsonElement("message")]
        public string Message { get; set; }

        public PastaRecord(string message)
        {
            Message = message;
        }
    }
}