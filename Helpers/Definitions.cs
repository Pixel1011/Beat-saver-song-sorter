using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beat_saber_Sorter.Helpers
{

    public class Songs
    {
        public List<BSSong> docs { get; set; } = new List<BSSong>();
    }
    public class BSSong
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public Uploader uploader { get; set; }
        public Metadata metadata { get; set; }
        public Stats stats { get; set; }
        public string? uploaded { get; set; }
        public bool? automapper { get; set; }
        public bool? ranked { get; set; }
        public bool? qualified { get; set; }
        public List<Version> versions { get; set; } = new List<Version>();
        public Curator curator { get; set; }
        public string? curatedAt { get; set; }
        public string? createdAt { get; set; }
        public string? updatedAt { get; set; }
        public string? lastPublishedAt { get; set; }
        public List<string> tags { get; set; } = new List<string>();
        public long timestamp { get; set; }
    }

    public class Uploader
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string avatar { get; set; }
        public string type { get; set; }
        public bool? admin { get; set; }
        public bool? curator { get; set; }
        public bool? verifiedMapper { get; set; }
        public string playlistUrl { get; set; }
        public bool? uniqueSet { get; set; }
    }

    public class Metadata
    {
        public double? bpm { get; set; }
        public int? duration { get; set; }
        public string songName { get; set; }
        public string songSubName { get; set; }
        public string songAuthorName { get; set; }
        public string levelAuthorName { get; set; }
    }

    public class Stats
    {
        public int? plays { get; set; }
        public int? downloads { get; set; }
        public int? upvotes { get; set; }
        public int? downvotes { get; set; }
        public double? score { get; set; }
        public int? reviews { get; set; }
        public string sentiment { get; set; }
    }

    public class Version
    {
        public string hash { get; set; }
        public string state { get; set; }
        public string? createdAt { get; set; }
        public int? sageScore { get; set; }
        public List<Diff> diffs { get; set; } = new List<Diff>();
        public string downloadURL { get; set; }
        public string coverURL { get; set; }
        public string previewURL { get; set; }
        public string key { get; set; }
    }
    // Version diff
    public class Diff
    {
        public double? njs { get; set; }
        public double? offset { get; set; }
        public int? notes { get; set; }
        public int? bombs { get; set; }
        public int? obstacles { get; set; }
        public double? nps { get; set; }
        public double? length { get; set; }
        public string characteristic { get; set; }
        public string difficulty { get; set; }
        public int? events { get; set; }
        public bool? chroma { get; set; } 
        public bool? me { get; set; } // mapping extensions
        public bool? ne { get; set; } // noodle extensions
        public bool? cinema { get; set; } 
        public double? seconds { get; set; }
        public ParitySummary paritySummary { get; set; }
        public int? maxScore { get; set; }
        public string label { get; set; }
        public double? stars { get; set; }
    }
    public class ParitySummary
    {
        public int? errors { get; set; }
        public int? warns { get; set; }
        public int? resets { get; set; }
    }
    //

    public class Curator
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string avatar { get; set; }
        public string type { get; set; }
        public bool? admin { get; set; }
        public bool? curator { get; set; }
        public bool? verifiedMapper { get; set; }
        public string playlistUrl { get; set; }
    }
}
