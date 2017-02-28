namespace MongoDBClient {

// Import MongoDB driver: necessary for using the client
using MongoDB.Driver;
using System;
//using System.Threading.Tasks;
using MongoDB.Bson;

    public class ClientReader {

        // MongoDB client is thread safe, so we can use the reference only
        protected static IMongoClient _client;        

        public void Connect() {

        var _clientSettings = new MongoClientSettings 
        { 
            // Credentials = new[] { mongoCredential }, 
            // A connection pool is created automatically by the client per server/servers
            Server = new MongoServerAddress("mongodb", 27017),
            
            // MongoDB drivers use a Server Selection algorithm to choose which replica set member to use or, 
            // when connected to multiple mongos instances, which mongos instance to use.
            // Server selection occurs once per operation.
            // Example in case of multiple servers 
            // Servers = new MongoServerAddress[] {
            //     new MongoServerAddress("localhost", 27018),
            //     new MongoServerAddress("localhost", 27019),
            //     new MongoServerAddress("localhost", 27020),
            // },

            // The name of the replica set
            // ReplicaSetName = "rs",

            // How the client identifies itself with MongoDB
            ApplicationName = "MongoDB-Client-C#-reader",

            /*
                Automatic	0	Automatically determine how to connect.
                Direct	    1  	Connect directly to a server.
                ReplicaSet	2	Connect to a replica set.
                ShardRouter	3	Connect to one or more shard routers.
                Standalone	4	Connect to a standalone server.
             */
            ConnectionMode = ConnectionMode.Automatic,

            // Minumum amount of connections in the pool
            MinConnectionPoolSize = 5,

            // Maximum amount of connections in the pool
            MaxConnectionPoolSize = 15,

            // ReadPreference support for MaxStaleness
            // ReadPreference has a new MaxStaleness property that can be used when reading 
            // from secondaries to prevent reading from secondaries that are too far behind the primary.
            // https://docs.mongodb.com/manual/reference/read-concern/
            ReadConcern = ReadConcern.Local,

            // From which type of server to read from
            // By default, a client directs its read operations to the primary member in a replica set.
            // Exercise care when specifying read preferences: 
            // Modes other than primary may return stale data because with asynchronous replication, 
            // data in the secondary may not reflect the most recent write operations.
            // https://docs.mongodb.com/manual/core/read-preference/#maxstalenessseconds
            ReadPreference = ReadPreference.PrimaryPreferred,
            
            UseSsl = false,

            // Timeout while sending an heartbeat
            HeartbeatTimeout = TimeSpan.FromSeconds(5),

            // Interval between heartbeats
            HeartbeatInterval = TimeSpan.FromSeconds(7),

        };

            _client = new MongoClient(_clientSettings);
            
        }

        public void Get(string db, string collection, FilterDefinition<BsonDocument> filter) {
            var _db = _client.GetDatabase(db);
            if (_db != null) {
                var _collection = _db.GetCollection<BsonDocument>(collection);                
                var result = _collection.Find(filter);
                Console.WriteLine(result.Count());
                Console.WriteLine(result.First().ToJson()) ;
            }
        }
    }
}