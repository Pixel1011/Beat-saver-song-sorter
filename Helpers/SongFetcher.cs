using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beat_saber_Sorter.Helpers
{
    internal class SongFetcher
    {
        public const string URL = "https://beatsaver.com/api/";
        public static readonly Dictionary<string, string> ENDPOINTS = new()
        {
            { "search", "search/text" }, // /{pagenum}
            { "maps", "maps/latest" },
            { "song", "maps/id" } // /{id}
        };
        Requester Req { get; set; }
        DB Db { get; set; }
        public SongFetcher(ref DB db, ref Requester req)
        {
            this.Req = req;
            this.Db = db;
        }
        public BSSong? GetSong(string id)
        {
            E err = new E { error = "" };
            string json = Req.Get(URL + ENDPOINTS["song"] + $"/{id}");
            try
            {
                err = JsonConvert.DeserializeObject<E>(json);
                if (err.error == "Not Found")
                {
                    Db.Table.DeleteOne(Builders<BSSong>.Filter.Eq("_id", id));
                    return null;
                }
            } catch
            {
                // too many requests so we sit around for a bit
                Thread.Sleep(5000);
                return GetSong(id);
            }

            BSSong song = JsonConvert.DeserializeObject<BSSong>(json)!;
            return song;
        }
        public Songs? GetSongs(string? timeString)
        {
            string before = "";
            if (timeString != null) before = $"&before={Uri.EscapeDataString(timeString)}";
            if (before == null) throw new Exception("what");
            
            string json = Req.Get(URL + ENDPOINTS["maps"] + $"?automapper=true&sort=CREATED{before}");
            E err = new E { error = "" };
            try
            {
                err = JsonConvert.DeserializeObject<E>(json);
            } catch
            {
                // too many requests so we sit around for a bit
                Thread.Sleep(5000);
                return GetSongs(timeString);
            }
            if (err.error != null) throw new Exception(err.error);
            
            Songs songs = JsonConvert.DeserializeObject<Songs>(json);
            return songs;
        }
        public struct E
        {
            public string error { get; set; }
        }
    }
}
