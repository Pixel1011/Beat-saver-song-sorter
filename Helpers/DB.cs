using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beat_saber_Sorter.Helpers
{
    internal class DB
    {
        public MongoClient Client { get; private set; }
        public IMongoDatabase Database { get; private set; }
        public IMongoCollection<BSSong> Table { get; private set; }
        public DB()
        {
            MongoClientSettings Settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017/");
            Settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            Client = new MongoClient(Settings);
            Database = Client.GetDatabase("BSSongs");
            Table = Database.GetCollection<BSSong>("Songs");
        }

        public void Insert(BSSong song)
        {
            song.timestamp = DateTimeOffset.Parse(song.createdAt).ToUnixTimeSeconds();
            Table.InsertOne(song);
        }
        public void InsertMany(List<BSSong> songs)
        {
            foreach (BSSong song in songs)
            {
                try
                {
                    Table.InsertOne(song);
                } catch
                {
                    continue;
                }
            }
        }
        public void UpdateSong(BSSong song)
        {
            song.timestamp = DateTimeOffset.Parse(song.createdAt).ToUnixTimeSeconds();
            Table.ReplaceOne(s => s.id == song.id, song);
        }
        
        public BSSong GetSongFromID(string id)
        {
            FilterDefinition<BSSong> filter = Builders<BSSong>.Filter.Eq("_id", id);
            List<BSSong> result = Table.Find(filter).ToList();
            return result[0];

        }
    }
}
