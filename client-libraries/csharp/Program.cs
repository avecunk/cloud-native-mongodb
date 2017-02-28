using System;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var _document = new BsonDocument
{
    { "address" , new BsonDocument
        {
            { "street", "2 Avenue" },
            { "zipcode", "10075" },
            { "building", "1480" },
            { "coord", new BsonArray { 73.9557413, 40.7720266 } }
        }
    },
    { "borough", "Manhattan" },
    { "cuisine", "Italian" },
    { "grades", new BsonArray
        {
            new BsonDocument
            {
                { "date", new DateTime(2014, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                { "grade", "A" },
                { "score", 11 }
            },
            new BsonDocument
            {
                { "date", new DateTime(2014, 1, 6, 0, 0, 0, DateTimeKind.Utc) },
                { "grade", "B" },
                { "score", 17 }
            }
        }
    },
    { "name", "Vella" },
    { "restaurant_id", "41704620" }
};
            
            MongoDBClient.ClientWriter _writer = new MongoDBClient.ClientWriter();
            _writer.Connect();
            _writer.Insert("test", "restaurants", _document);

            MongoDBClient.ClientReader reader = new MongoDBClient.ClientReader();
            reader.Connect();
            var filter = Builders<BsonDocument>.Filter.Eq("address.zipcode", "10075");
            reader.Get("test", "restaurants", filter);

        }
    }
}
