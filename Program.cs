
using Beat_saber_Sorter.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using NAudio.Wave;
using System.Diagnostics;


namespace Beat_saber_Sorter
{
    internal class Program
    {
        static Requester req = new Requester();
        static DB db = new DB();
        static SongFetcher Fetcher = new SongFetcher(ref db, ref req);

        static BSSong? lastLookedAtSong = null;

        static void Main(string[] args)
        {

            while (true)
            {
                Tuple<int, string[]> tuple = ShowMenu();
                string[] arg = tuple.Item2;
                switch (tuple.Item1)
                {
                    case 1:
                        LoadSongsToDB(arg);
                        break;
                    case 2:
                        FilterSongs(arg);
                        break;
                    case 3:
                        InspectSong(arg);
                        break;
                    case 4:
                        InstallSong(arg);
                        break;
                    case 5:
                        PreviewSong(arg);
                        break;
                    case 6:
                        ListenToPreview(arg);
                        break;
                    case 7:
                        Test(arg);
                        break;
                    case 8:
                        Environment.Exit(0);
                        return;
                }
            }
        }

        static Tuple<int, string[]> ShowMenu()
        {
            string input = "";
            bool validated = true;
            string option = "";
            do
            {
                Console.ForegroundColor = ConsoleColor.White;
                if (!validated) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid input, please try again"); }
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("[1] Load x pages to DB ");
                Console.Write("[2] Search for song(s) ");
                Console.Write("[3] Inspect song by id ");
                Console.Write("[4] Install song ");
                Console.Write("\n");
                Console.Write("[5] Preview song by id in browser ");
                Console.Write("[6] Listen to song preview ");
                Console.Write("[7] Test ");
                Console.Write("[8] Exit");
                Console.Write("\n");

                Console.ForegroundColor = ConsoleColor.White;
                input = Console.ReadLine();
                option = input.Split(' ')[0];
                validated = option == "1" || option == "2" || option == "3" || option == "4" || option == "5" || option == "6" || option == "7" || option == "8";
            } while (!validated);

            return new Tuple<int, string[]>(int.Parse(option), input.Split(' '));

        }
    
        static void LoadSongsToDB(string[] args)
        {
            if (args.Length == 1) { Console.WriteLine("Please enter a number of pages to load"); return; }
            try { int.Parse(args[1]); } catch { Console.WriteLine("Please enter a valid number of pages to load"); return; }
            int pages = int.Parse(args[1]) - 1;
            if (pages < 0) { Console.WriteLine("Please enter a valid number of pages to load"); return; }

            int songCount = 1;
            Songs songs = Fetcher.GetSongs(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "+00:00")!;

            while (songs.docs.Count != 0 && (songCount / 20) <= pages)
            {
                foreach (BSSong song in songs.docs)
                {
                    // i had a try catch here. and it kept stuttering every page of 20 songs i am confused but sure
                    if (db.Table.FindSync(Builders<BSSong>.Filter.Eq("_id", song.id)).ToList().Count == 0)
                    {
                        db.Insert(song);
                        OutputFormattedSong(song, songCount, "Added song: ");
                    }
                    else
                    {
                        OutputFormattedSong(song, songCount, "Updating song: ");
                        db.UpdateSong(song);
                    }
                    songCount++;
                    continue;
                }
                // end foreach
                Songs songs2 = Fetcher.GetSongs(songs.docs.Last().createdAt)!;
                songs = songs2;
            }
            // end while
        }
        // features, before, after, during, pages, sort by value (highest to lowest), filter by value with regex or exact or less than or greater than
        static void FilterSongs(string[] args)
        {
            //if (args.Length == 1) { Console.WriteLine("Please enter a filter"); return; }
            var filters = args.Skip(1).ToArray();


            // sort through arguments
            FilterDefinition<BSSong> filter = Builders<BSSong>.Filter.Empty;
            // by default sort by upvotes
            SortDefinition<BSSong> sort = Builders<BSSong>.Sort.Descending("stats.upvotes");
            int pagenum = 1;

            for (int i = 0; i < filters.Length; i++)
            {
                string f = filters[i].Trim();
                if (f.Contains("sort"))
                {
                    // eg sort:value
                    // could have first 3 values after : to determine whether ascending descending or whatever
                    string[] splitSort = f.Split(':');
                    var SortByValue = splitSort[1];
                    sort = Builders<BSSong>.Sort.Descending(SortByValue);

                }
                else if (f.Contains("page"))
                {
                    // page:num
                    string[] splitPage = f.Split(':');
                    string p = splitPage[1];
                    pagenum = int.Parse(p);
                }
                else if (f.Contains("before"))
                {
                    // before:year
                    string[] timeFilter = f.Split(':');
                    var timeValue = timeFilter[1];
                    filter &= Builders<BSSong>.Filter.Lte("timestamp", DateTimeOffset.FromUnixTimeMilliseconds(0).AddYears(int.Parse(timeValue) - 1970).ToUnixTimeSeconds());
                }
                else if (f.Contains("after"))
                {
                    // after:year
                    string[] timeFilter = f.Split(':');
                    var timeValue = timeFilter[1];
                    filter &= Builders<BSSong>.Filter.Gte("timestamp", DateTimeOffset.FromUnixTimeMilliseconds(0).AddYears(int.Parse(timeValue) - 1970).ToUnixTimeSeconds());
                }
                else if (f.Contains("during"))
                {
                    // during:year
                    string[] timeFilter = f.Split(':');
                    var timeValue = timeFilter[1];

                    long yearSecondsMin = DateTimeOffset.FromUnixTimeMilliseconds(0).AddYears(int.Parse(timeValue) - 1970).ToUnixTimeSeconds();
                    long yearSecondsMax = DateTimeOffset.FromUnixTimeSeconds(yearSecondsMin).AddYears(1).ToUnixTimeSeconds();
                    if (int.Parse(timeValue) != 1)
                    {
                        // filter if timestamp is within a year
                        filter &= Builders<BSSong>.Filter.Lt("timestamp", yearSecondsMax);
                        filter &= Builders<BSSong>.Filter.Gt("timestamp", yearSecondsMin);
                    }
                }
                else if (f.Contains(':'))
                {
                    // eg name:cool-song
                    // eg field:regex
                    string[] splitFilter = f.Split(':');
                    string filterField = splitFilter[0];
                    string filterValue = "";
                    foreach (string str in splitFilter.Skip(1))
                    {
                        filterValue += str;
                    }
                    filter &= Builders<BSSong>.Filter.Regex(filterField, new BsonRegularExpression(filterValue, "i"));
                }
                else if (f.Contains('='))
                {
                    // eg name=cool-song
                    string[] splitFilter = f.Split('=');
                    var filterField = splitFilter[0];
                    var filterValue = splitFilter[1].Replace("-", " ");
                    if (filterValue == "true" || filterValue == "false")
                    {
                        filter &= Builders<BSSong>.Filter.Eq(filterField, bool.Parse(filterValue));
                    } else
                    {
                        filter &= Builders<BSSong>.Filter.Eq(filterField, filterValue);
                    }
                }
                else if (f.Contains('<'))
                {
                    string[] splitFilter = f.Split('<');
                    var filterField = splitFilter[0];
                    var filterValue = splitFilter[1];
                    filter &= Builders<BSSong>.Filter.Lte(filterField, filterValue);
                }
                else if (f.Contains('>'))
                {
                    string[] splitFilter = f.Split('>');
                    var filterField = splitFilter[0];
                    var filterValue = splitFilter[1];
                    filter &= Builders<BSSong>.Filter.Gte(filterField, filterValue);
                }
            }

            var songsFind = db.Table.Find(filter);
            var songsSort = songsFind.Sort(sort);
            int totalPages = (int)Math.Ceiling(songsSort.CountDocuments() / 20d);
            var page = songsSort.Skip((pagenum - 1) * 20).Limit(20).ToList();
            if (totalPages == 0) { Console.WriteLine("No pages found"); return; }
            if (pagenum > totalPages) { pagenum = 1; page = songsSort.Skip((pagenum - 1) * 20).Limit(20).ToList(); }
            for (int i = 0; i < page.Count; i++)
            {
                BSSong result = Fetcher.GetSong(page[i].id);
                if (result == null) page.RemoveAt(i);
            }
            // write songs
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Showing page {pagenum}/" + totalPages + "\n");
            for (int i = 0; i < page.Count; i++)
            {
                OutputFormattedSong(page[i], i + 1 + ((pagenum - 1) * 20));
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

        }
        static void OutputFormattedSong(BSSong song, int index, string namePrefix = "")
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"[{index}] ");
            if (namePrefix != "")
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(namePrefix);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(song.name);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" (" + song.id + ") ");
            // greater 0
            if (song.stats.upvotes - song.stats.downvotes > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            // less than
            else if (song.stats.upvotes - song.stats.downvotes < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            // equal to
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            Console.Write("("+song.stats.upvotes + ") ");
            var chromeNoodle = findChromaOrNoodle(song);
            if (chromeNoodle.Item1)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write("(Chroma) ");
            }
            if (chromeNoodle.Item2)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write("(Noodle Extensions) ");
            }
            Console.Write("\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
        static void OutputExtendedFormattedSong(BSSong song)
        {
            // output name, id, desc, songname and author, map author, plays, upvotes, downvotes, when it was uploaded, whether ranked or not, if curated
            // tags and the difficulties it has
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(song.name);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" (" + song.id + ")\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(song.description);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Song: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(song.metadata.songName);
            if (song.metadata.songSubName != "")
            {
                Console.Write(" - " + song.metadata.songSubName);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" by ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(song.metadata.songAuthorName);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Map Creator: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(song.metadata.levelAuthorName);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Duration: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(TimeSpan.FromSeconds((double)song.metadata.duration).ToString(@"mm\:ss"));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("BPM: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(song.metadata.bpm);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Upvotes: ");

            Console.ForegroundColor = ConsoleColor.Green;
            if (song.stats.downvotes > song.stats.upvotes) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(song.stats.upvotes);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Downvotes: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(song.stats.downvotes);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Ranked: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(song.ranked);

            if (song.curator != null)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Curated by: ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(song.curator.name);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Difficulties: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(string.Join(", ", song.versions.Last().diffs.Select(x => x.difficulty)));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Tags: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(string.Join(", ", song.tags));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Uploaded: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(DateTimeOffset.Parse(song.createdAt!).ToString("dd:MM:yyyy"));

            Console.WriteLine();

        }
        static void InspectSong(string[] args, bool fromFilters = false, List<BSSong>? page = null)
        {
            string id = args[args.Length - 1];
            if (fromFilters)
            {
                // id is index number - page is not null
                id = page![int.Parse(id)].id;
            }
            BSSong song = Fetcher.GetSong(id);
            if (song == null) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Song not found from beatsaver"); return; }
            db.UpdateSong(song);
            OutputExtendedFormattedSong(song);
            // give install prompt
            string input = "";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Install song? Y/N");
            Console.ForegroundColor = ConsoleColor.White;
            input = Console.ReadLine().ToLower();
            if (input == "y")
            {
                InstallSong(new string[] { "", song.id });
            }
            lastLookedAtSong = song;
        }
        static void InstallSong(string[] args)
        {
            string id;
            if (args.Count() == 1 && lastLookedAtSong != null) 
            { 
                id = lastLookedAtSong.id;
            }
            else
            {
                id = args[1];
            }
            BSSong song = Fetcher.GetSong(id);
            if (song == null) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Song not found from beatsaver"); return; }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Installing {song.name} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"({id})");
            Process.Start(new ProcessStartInfo { Arguments = $"beatsaver://{id}", FileName = "explorer.exe" });
        }
        static void PreviewSong(string[] args)
        {
            string id;
            if (args.Count() == 1 && lastLookedAtSong != null)
            {
                id = lastLookedAtSong.id;
            }
            else
            {
                id = args[1];
            }
            BSSong song = Fetcher.GetSong(id);
            if (song == null) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Song not found from beatsaver"); return; }


            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Previewing ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{song.name} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"({id}) ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("in browser");

            Process.Start(new ProcessStartInfo($"https://skystudioapps.com/bs-viewer/?id={id}") { UseShellExecute = true });
            lastLookedAtSong = song;
        }
        static void ListenToPreview(string[] args)
        {
            string id;
            if (args.Count() == 1 && lastLookedAtSong != null)
            {
                id = lastLookedAtSong.id;
            }
            else
            {
                id = args[1];
            }
            BSSong song = Fetcher.GetSong(id);
            if (song == null) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Song not found from beatsaver"); return; }
            string mp3url = song.versions.Last().previewURL;
            using Stream ms = new MemoryStream();
            using Stream stream = req.GetStream(mp3url);
            byte[] buffer = new byte[100 * 1024];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            ms.Position = 0;
            using WaveStream blockAlignedStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(ms)));
            using WaveOut directOut = new WaveOut();
            directOut.Init(blockAlignedStream);
            directOut.Volume = 0.4f;
            directOut.Play();
            while (directOut.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
                if (Console.NumberLock) break; // can stop 
            }
            directOut.Dispose();
            lastLookedAtSong = song;
        }
        static void Test(string[] args)
        {
            FilterDefinition<BSSong> filter = Builders<BSSong>.Filter.Empty;
            filter &= Builders<BSSong>.Filter.Eq("versions.diffs.ne", true);

            // filter if timestamp is within a year
            var songsFind = db.Table.Find(filter);
            int totalPages = (int)Math.Ceiling(songsFind.CountDocuments() / 20d);

            var page = songsFind.Limit(20).ToList();
            if (page.Count == 0) { Console.WriteLine("No pages found"); return; }
            // write songs
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Showing page 1/" + totalPages + "\n");
            for (int i = 0; i < page.Count; i++)
            {
                OutputFormattedSong(page[i], i + 1);
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        static Tuple<bool,bool> findChromaOrNoodle(BSSong song)
        {
            bool Chroma = false;
            bool Noodle = false;
            for (int i = 0; i < song.versions.Count; i++) 
            {
                Helpers.Version v = song.versions[i];
                for (int j = 0; j < v.diffs.Count; j++)
                {
                    Diff diff = v.diffs[j];
                    if ((bool)diff.chroma!) Chroma = true;
                    if ((bool)diff.ne!) Noodle = true;
                    if (Chroma && Noodle) break;
                }
                if (Chroma && Noodle) break;
            }
            return new Tuple<bool, bool>(Chroma, Noodle);
        }
    }
}