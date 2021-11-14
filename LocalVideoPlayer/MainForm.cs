using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.TMDb;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;

namespace LocalVideoPlayer
{
    public partial class MainForm : Form
    {
        private const string lang = "en-US";
        private const string jsonFile = "Media.json";

        private ServiceClient client;
        private Media media;
        public MainForm()
        {
            InitializeComponent();

            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            //backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler( backgroundWorker1_ProgressChanged);
            backgroundWorker1.RunWorkerAsync();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine("backgroundWorker1 completed");
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            ProcessDirectory(@"C:\zMedia");
            bool update = CheckForUpdates();
            if (update)
            {
                Task buildCache = BuildCacheAsync();
                buildCache.Wait();
            }

            //load data 
        }

        private async Task BuildCacheAsync()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            // loop through media... check for identifying item only from api...if not there update
            using (ServiceClient client = new ServiceClient("c69c4effc7beb9c473d22b8f85d59e4c"))
            {
                for (int i = 0; i < media.Movies.Length; i++)
                {
                    if (media.Movies[i].Id != 0) continue;
                    LocalMovie localMovie = media.Movies[i];
                    Resources movieSearch = await client.SearchAsync(localMovie.Name, lang, false, 1, token);
                    //To-do: null check
                    localMovie.Id = movieSearch.Results.First().Id;
                    Movie movie = await client.Movies.GetAsync(localMovie.Id, lang, true, token);
                    localMovie.Poster = movie.Poster;
                    localMovie.Backdrop = movie.Backdrop;
                    localMovie.Overview = movie.Overview;
                    localMovie.Date = movie.ReleaseDate;
                }

                for (int i = 0; i < media.Shows.Length; i++)
                {
                    LocalShow localShow = media.Shows[i];

                    if(localShow.Id == 0)
                    {
                        Resources tvSearch = await client.SearchAsync(localShow.Name, lang, false, 1, token);
                        //To-do: null check
                        localShow.Id = tvSearch.Results.First().Id;
                        Show show = await client.Shows.GetAsync(localShow.Id, lang, true, token);
                        localShow.Overview = show.Overview;
                        localShow.Poster = show.Poster;
                        localShow.Backdrop = show.Backdrop;
                    }

                    int seasonIndex = 0;
                    for (int j = 0; j < localShow.Seasons.Length; j++)
                    {
                        LocalSeason localSeason = localShow.Seasons[i];

                        Season season = await client.Shows.GetSeasonAsync(localShow.Id, seasonIndex, lang, true, token);
                        //To-do: skipped "Specials"
                        if (!season.Name.Contains("Season"))
                        {
                            seasonIndex++;
                            season = await client.Shows.GetSeasonAsync(localShow.Id, seasonIndex, lang, true, token);
                        }

                        if (localSeason.Poster == null)
                        {
                            localSeason.Poster = season.Poster;
                            localSeason.Date = season.AirDate;
                        }

                        Episode[] episodes = season.Episodes.ToArray();
                        LocalEpisode[] localEpisodes = localSeason.Episodes;

                        for (int k = 0; k < episodes.Length; k++)
                        {
                            if (localEpisodes[i].Backdrop != null) continue;
                            if (episodes[i].Id == localEpisodes[i].Id && episodes[i].Name.Equals(localEpisodes[i].Name))
                            {
                                Episode episode = episodes[i];
                                LocalEpisode localEpisode = localEpisodes[i];

                                localEpisode.Overview = episode.Overview;
                                localEpisode.Backdrop = episode.Backdrop;
                                localEpisode.Date = episode.AirDate;
                                //To-do: if season does not match? if one or the other matches error checking
                            }
                        }
                        seasonIndex++;
                    }
                }
            }

            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);
        }

        private bool CheckForUpdates()
        {
            string jsonString = File.ReadAllText(jsonFile);
            Media prevMedia = JsonConvert.DeserializeObject<Media>(jsonString);
            if (prevMedia == null)
            {
                return true;
            }
            return media.Compare(prevMedia);
        }

        private void ProcessDirectory(string targetDir)
        {
            Console.WriteLine("Process directory '{0}'.", targetDir);

            //expect only TV and MOVIES folder
            string[] subdirectoryEntries = Directory.GetDirectories(targetDir);
            string moviesDir = subdirectoryEntries[0];
            string tvDir = subdirectoryEntries[1];
            int moviesCount = Directory.GetDirectories(moviesDir).Length;
            int tvCount = Directory.GetDirectories(tvDir).Length;
            media = new Media(moviesCount, tvCount);

            string[] movieEntries = Directory.GetDirectories(moviesDir);
            for (int i = 0; i < movieEntries.Length; i++)
            {

                media.Movies[i] = ProcessMovieDirectory(movieEntries[i]);
            }

            string[] tvEntries = Directory.GetDirectories(tvDir);
            for (int i = 0; i < tvEntries.Length; i++)
            {
                media.Shows[i] = ProcessTvDirectory(tvEntries[i]);
            }

        }

        static async Task DownloadImage(string filename, string localpath, CancellationToken cancellationToken)
        {
            if (!File.Exists(localpath))
            {
                string folder = Path.GetDirectoryName(localpath);
                Directory.CreateDirectory(folder);

                var storage = new StorageClient();
                using (var fileStream = new FileStream(localpath, FileMode.Create,
                    FileAccess.Write, FileShare.None, short.MaxValue, true))
                {
                    try { await storage.DownloadAsync(filename, fileStream, cancellationToken); }
                    catch (Exception ex) { System.Diagnostics.Trace.TraceError(ex.ToString()); }
                }
            }
        }
        private LocalMovie ProcessMovieDirectory(string targetDir)
        {
            Console.WriteLine(targetDir);
            string[] path = targetDir.Split('\\');
            string name = path[path.Length - 1].Split('-')[0];
            LocalMovie movie = new LocalMovie(name.Trim(), targetDir);
            return movie;
        }

        private LocalShow ProcessTvDirectory(string targetDir)
        {
            Console.WriteLine(targetDir);
            string[] path = targetDir.Split('\\');
            string name = path[path.Length - 1].Split('-')[0];
            LocalShow show = new LocalShow(name.Trim(), targetDir);
            string[] seasonEntries = Directory.GetDirectories(targetDir);
            show.Seasons = new LocalSeason[seasonEntries.Length];
            for (int i = 0; i < seasonEntries.Length; i++)
            {
                LocalSeason season = new LocalSeason(i + 1);
                string[] episodeEntries = Directory.GetFiles(seasonEntries[i]);
                season.Episodes = new LocalEpisode[episodeEntries.Length];
                for (int j = 0; j < episodeEntries.Length; j++)
                {
                    string[] namePath = episodeEntries[j].Split('\\');
                    string[] episodeNameNumber = namePath[namePath.Length - 1].Split('-');
                    LocalEpisode episode = new LocalEpisode(Int32.Parse(episodeNameNumber[0].Trim()), episodeNameNumber[1].Trim());
                    season.Episodes[j] = episode;
                }
                show.Seasons[i] = season;
            }
            return show;
        }
    }
}
