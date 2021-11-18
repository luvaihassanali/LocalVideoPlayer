﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalVideoPlayer
{
    public partial class MainForm : Form
    {
        private const string jsonFile = "Media.json";
        private const string apiKey = "?api_key=c69c4effc7beb9c473d22b8f85d59e4c";
        private const string apiUrl = "https://api.themoviedb.org/3/";
        private const string apiImageUrl = "http://image.tmdb.org/t/p/original";
        private const string tvSearch = "https://api.themoviedb.org/3/search/tv?api_key=c69c4effc7beb9c473d22b8f85d59e4c&query=";
        private const string movieSearch = "https://api.themoviedb.org/3/search/movie?api_key=c69c4effc7beb9c473d22b8f85d59e4c&query=";

        private string tvGet = "https://api.themoviedb.org/3/tv/{tv_id}?api_key=c69c4effc7beb9c473d22b8f85d59e4c";
        private string tvSeasonGet = "https://api.themoviedb.org/3/tv/{tv_id}/season/{season_number}?api_key=c69c4effc7beb9c473d22b8f85d59e4c";
        private string movieGet = "https://api.themoviedb.org/3/movie/{movie_id}?api_key=c69c4effc7beb9c473d22b8f85d59e4c";
        private string bufferString = "";

        private Media media;
        private Label movieLabel;
        private Label tvLabel;

        public MainForm()
        {
            PlayerForm p = new PlayerForm(@"C:\zMedia\MOVIES\Harry Potter and the Chamber of Secrets\Harry potter and the chamber of secrets.avi");
            p.Show();
            /*
            InitializeComponent();

            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            //backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler( backgroundWorker1_ProgressChanged);
            backgroundWorker1.RunWorkerAsync();*/
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
            Console.WriteLine("Init gui");
            InitGui();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            ProcessDirectory(@"C:\zMedia");
            bool update = CheckForUpdates();
            Console.WriteLine("UPDATE: " + update);
            if (update)
            {
                Task buildCache = BuildCacheAsync();
                buildCache.Wait();
            }
        }

        private void InitGui()
        {
            //To-do: no media exists
            Panel currentPanel = null;
            int count = 0;
            int panelCount = 0;

            for (int i = 0; i < media.Movies.Length; i++)
            {
                if (count == 6) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Transparent;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "movie" + panelCount;
                    panelCount++;
                    this.Controls.Add(currentPanel);
                    this.Controls.SetChildIndex(currentPanel, 0);
                }

                PictureBox movieBox = new PictureBox();
                movieBox.Width = this.Width / 6;
                movieBox.Height = 450;
                string imagePath = media.Movies[i].Poster;
                movieBox.Image = Image.FromFile(imagePath);
                movieBox.BackColor = Color.Transparent;
                movieBox.Left = movieBox.Width * currentPanel.Controls.Count;
                movieBox.Cursor = Cursors.Hand;
                movieBox.SizeMode = PictureBoxSizeMode.StretchImage;
                movieBox.Padding = new Padding(10, 10, 40, 10);
                movieBox.Name = media.Movies[i].Name;
                movieBox.Click += movieBox_Click;
                currentPanel.Controls.Add(movieBox);
                count++;
            }

            movieLabel = new Label();
            movieLabel.Text = "Movies";
            movieLabel.Dock = DockStyle.Top;
            movieLabel.Paint += headerLabel_Paint;
            movieLabel.AutoSize = true;
            movieLabel.Name = "movieLabel";
            this.Controls.Add(movieLabel);

            currentPanel = null;
            count = 0;
            panelCount = 0;
            for (int i = 0; i < media.TvShows.Length; i++)
            {
                if (count == 6) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Transparent;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "tv" + panelCount;
                    panelCount++;
                    this.Controls.Add(currentPanel);
                    this.Controls.SetChildIndex(currentPanel, 3);
                }

                PictureBox tvShowBox = new PictureBox();
                tvShowBox.Width = this.Width / 6;
                tvShowBox.Height = 450;
                string imagePath = media.TvShows[i].Poster;
                tvShowBox.Image = Image.FromFile(imagePath);
                tvShowBox.BackColor = Color.Transparent;
                tvShowBox.Left = tvShowBox.Width * currentPanel.Controls.Count;
                tvShowBox.Cursor = Cursors.Hand;
                tvShowBox.SizeMode = PictureBoxSizeMode.StretchImage;
                tvShowBox.Padding = new Padding(10, 10, 40, 10);
                tvShowBox.Name = media.TvShows[i].Name;
                tvShowBox.Click += tvShowBox_Click;
                currentPanel.Controls.Add(tvShowBox);
                count++;
            }

            tvLabel = new Label();
            tvLabel.Text = "TV Shows";
            tvLabel.Dock = DockStyle.Top;
            tvLabel.Paint += headerLabel_Paint;
            tvLabel.AutoSize = true;
            tvLabel.Name = "tvLabel";
            this.Controls.Add(tvLabel);

            //To-do: clean up dynamically created controls

        }

        private void tvShowBox_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void movieBox_Click(object sender, EventArgs e)
        {
            Form movieForm = new Form();

            PictureBox p = sender as PictureBox;
            Movie movie = GetMovie(p.Name);

            movieForm.Width = (int)(this.Width / 1.75);
            movieForm.Height = this.Height;
            //Size maxSize = new Size(800, 800);
            //movieForm.MaximumSize = maxSize;
            movieForm.AutoScroll = true;
            movieForm.AutoSize = true;
            //movieForm.Text = header;
            movieForm.StartPosition = FormStartPosition.CenterScreen;
            movieForm.BackColor = SystemColors.Desktop;
            movieForm.ForeColor = SystemColors.Control;

            Font f = new Font("Arial", 14, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);

            Label textLabel = new Label() { Text = movie.Overview };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = f2;
            textLabel.AutoSize = true;
            Padding p2 = new Padding(5, 20, 10, 0);
            textLabel.Padding = p2;
            textLabel.MaximumSize = movieForm.Size;
            movieForm.Controls.Add(textLabel);

            Label headerLabel = new Label() { Text = movie.Name + " (" + movie.Date.GetValueOrDefault().Year + ")" };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = f;
            headerLabel.AutoSize = true;
            Padding p3 = new Padding(5, 20, 0, 0);
            headerLabel.Padding = p3;
            movieForm.Controls.Add(headerLabel);

            PictureBox movieBackdropBox = new PictureBox();
            Console.WriteLine(movieForm.Width);
            movieBackdropBox.Height = 622; //3840 x 2160 1920 x 1080
            string imagePath = movie.Backdrop;
            movieBackdropBox.Image = Image.FromFile(imagePath);
            movieBackdropBox.BackColor = Color.Red;
            //movieBackdropBox.Left = movieBackdropBox.Width * currentPanel.Controls.Count;
            movieBackdropBox.Dock = DockStyle.Top;
            movieBackdropBox.Cursor = Cursors.Hand;
            movieBackdropBox.SizeMode = PictureBoxSizeMode.StretchImage;
            movieBackdropBox.Name = movie.Path;
            movieBackdropBox.Click += movieBackdropBox_Click;
            movieForm.Controls.Add(movieBackdropBox);

            movieForm.Deactivate += (s, ev) => { movieForm.Close(); movieForm.Dispose(); };
            movieForm.Show();
        }

        private void movieBackdropBox_Click(object sender, EventArgs e)
        {
            PictureBox p = sender as PictureBox;
            string path = p.Name;
            LaunchVlc(path);
        }

        private void LaunchVlc(string path)
        {
            Form playerForm = new PlayerForm(path);
            playerForm.Show();
        }

        private Movie GetMovie(object name)
        {
            for (int i = 0; i < media.Movies.Length; i++)
            {
                if(media.Movies[i].Name.Equals(name))
                {
                    return media.Movies[i];
                }
            }
            return null;
        }

        private void CustomMessageForm(string header, string message)
        {
            Form prompt = new Form();
            prompt.Width = 800;
            prompt.Height = 100;
            Size maxSize = new Size(this.Width, 800);
            prompt.MaximumSize = maxSize;
            prompt.AutoScroll = true;
            prompt.AutoSize = true;
            prompt.Text = header;
            prompt.StartPosition = FormStartPosition.CenterScreen;
            prompt.BackColor = SystemColors.Desktop;
            prompt.ForeColor = SystemColors.Control;
            
            Font f = new Font("Arial", 14, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);

            Label textLabel = new Label() { Text = message };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = f2;
            textLabel.AutoSize = true;
            Padding p2 = new Padding(10, 0, 0, 10);
            textLabel.Padding = p2;
            prompt.Controls.Add(textLabel);

            Label headerLabel = new Label() { Text = header };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = f;
            headerLabel.AutoSize = true;
            Padding p = new Padding(0, 0, 0, 15);
            headerLabel.Padding = p;
            prompt.Controls.Add(headerLabel);

            Button confirmation = new Button() { Text = "Ok" };
            confirmation.AutoSize = true;
            confirmation.Font = f2;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = Cursors.Hand;
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(confirmation);
            prompt.ShowDialog();
            prompt.Dispose();
        }

        private int ShowOptionsForm(string item, string[][] info, DateTime?[] dates)
        {
            Form prompt = new Form();
            prompt.Width = 800;
            prompt.Height = 400;
            Size maxSize = new Size(800, 800);
            prompt.MaximumSize = maxSize;
            prompt.AutoScroll = true;
            prompt.AutoSize = true;
            prompt.Text = "Selection required";
            prompt.StartPosition = FormStartPosition.CenterScreen;
            prompt.BackColor = SystemColors.Desktop;
            prompt.ForeColor = SystemColors.Control;
            Font f = new Font("Arial", 14, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);
            Label textLabel = new Label() { Text = "Choose a selection for: " + item };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = f;
            textLabel.AutoSize = true;
            Padding p = new Padding(0, 0, 0, 15);
            textLabel.Padding = p;

            List<Control> controls = new List<Control>();

            for (int i = 0; i < info[0].Length; i++)
            {
                RadioButton r1 = new RadioButton { Text = info[0][i] + " (" + dates[i].GetValueOrDefault().Year + ")" };
                r1.Dock = DockStyle.Top;
                r1.Font = f2;
                r1.AutoSize = true;
                r1.Cursor = Cursors.Hand;
                Padding p2 = new Padding(0, 0, 0, 10);
                r1.Padding = p2;
                r1.Name = info[1][i];
                controls.Add(r1);

                Label l1 = new Label() { Text = info[2][i].Equals(String.Empty) ? "No description." : info[2][i] };
                l1.Dock = DockStyle.Top;
                l1.Font = f2;
                l1.AutoSize = true;
                Padding p3 = new Padding(10, 0, 0, 10);
                l1.Padding = p3;
                Size s = new Size(prompt.Width - 20, prompt.Height);
                l1.MaximumSize = s;
                controls.Add(l1);

            }

            Button confirmation = new Button() { Text = "Ok" };
            confirmation.AutoSize = true;
            confirmation.Font = f2;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = Cursors.Hand;
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(confirmation);
            controls.Reverse();
            foreach (Control c in controls)
            {
                prompt.Controls.Add(c);
            }
            prompt.Controls.Add(textLabel);

            prompt.ShowDialog();
            prompt.Dispose();

            int id = 0;
            foreach (Control c in controls)
            {
                RadioButton btn = c as RadioButton;
                if (btn != null)
                {
                    if (btn.Checked)
                    {
                        id = Int32.Parse(btn.Name);
                    }
                }
            }
            if (id == 0) throw new ArgumentNullException();

            return id;
        }

        private async Task BuildCacheAsync()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // loop through media... check for identifying item only from api...if not there update
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                for (int i = 0; i < media.Movies.Length; i++)
                {
                    //if id is init we skip
                    if (media.Movies[i].Id != 0) continue;

                    Movie movie = media.Movies[i];
                    Console.WriteLine("movie: " + movie.Name + " (" + movieSearch + movie.Name + ")");

                    string movieResourceString = client.DownloadString(movieSearch + movie.Name);
                    Console.WriteLine(movieResourceString);

                    JObject movieObject = JObject.Parse(movieResourceString);
                    int totalResults = (int)movieObject["total_results"];

                    if (totalResults == 0)
                    {
                        CustomMessageForm("Error", "No movie found for: " + movie.Name);
                    }
                    else if (totalResults != 1)
                    {
                        int actualResults = (int)((JArray)movieObject["results"]).Count();
                        string[] names = new string[actualResults];
                        string[] ids = new string[actualResults];
                        string[] overviews = new string[actualResults];
                        DateTime?[] dates = new DateTime?[actualResults];

                        for (int j = 0; j < actualResults; j++)
                        {
                            names[j] = (string)movieObject["results"][j]["title"];
                            ids[j] = (string)movieObject["results"][j]["id"];
                            overviews[j] = (string)movieObject["results"][j]["overview"];
                            DateTime temp;
                            dates[j] = DateTime.TryParse((string)movieObject["results"][j]["release_date"], out temp) ? temp : DateTime.MinValue.AddHours(9);
                        }

                        string[][] info = new string[][] { names, ids, overviews };
                        movie.Id = ShowOptionsForm(movie.Name, info, dates);
                    }
                    else
                    {
                        movie.Id = (int)movieObject["results"][0]["id"];
                    }

                    string movieString = client.DownloadString(movieGet.Replace("{movie_id}", movie.Id.ToString()));
                    Console.WriteLine(movieString);
                    movieObject = JObject.Parse(movieString);

                    if (String.Compare(movie.Name.Replace(":", ""), ((string)movieObject["title"]).Replace(":", ""), System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
                    {
                        movie.Backdrop = (string)movieObject["backdrop_path"];
                        movie.Poster = (string)movieObject["poster_path"];
                        movie.Overview = (string)movieObject["overview"];
                        DateTime tempDate;
                        movie.Date = DateTime.TryParse((string)movieObject["release_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);
                        
                        if (movie.Backdrop != null)
                        {
                            await SaveImage(movie.Backdrop, movie.Name, true, token);
                            movie.Backdrop = bufferString;
                        }

                        if (movie.Poster != null)
                        {
                            await SaveImage(movie.Poster, movie.Name, true, token);
                            movie.Poster = bufferString;
                        }
                    }
                    else
                    {
                        string message = "Local movie name does not match retrieved data. Renaming file '" + movie.Name.Replace(":", "") + "' to '" + ((string)movieObject["title"]).Replace(":", "") + "'.";
                        CustomMessageForm("Warning", message);

                        string oldPath = movie.Path;
                        string[] fileNamePath = oldPath.Split('\\');
                        string fileName = fileNamePath[fileNamePath.Length - 1];
                        string extension = fileName.Split('.')[1];
                        string newFileName = ((string)movieObject["title"]).Replace(":", "");
                        string newPath = oldPath.Replace(fileName, newFileName + "." + extension);
                        File.Move(oldPath, newPath);

                        movie.Path = newPath;
                        movie.Name = newFileName;
                        movie.Id = (int)movieObject["id"];
                        movie.Backdrop = (string)movieObject["backdrop_path"];
                        movie.Poster = (string)movieObject["poster_path"];
                        movie.Overview = (string)movieObject["overview"];
                        DateTime tempDate;
                        movie.Date = DateTime.TryParse((string)movieObject["release_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                        if (movie.Backdrop != null)
                        {
                            await SaveImage(movie.Backdrop, movie.Name, true, token);
                            movie.Backdrop = bufferString;
                        }

                        if (movie.Poster != null)
                        {
                            await SaveImage(movie.Poster, movie.Name, true, token);
                            movie.Poster = bufferString;
                        }
                    }
                }

                for (int i = 0; i < media.TvShows.Length; i++)
                {
                    TvShow tvShow = media.TvShows[i];
                    Console.WriteLine("tv show: " + tvShow.Name + " (" + tvSearch + tvShow.Name + ")");
                    //if id is not 0 expected to be init
                    if (tvShow.Id == 0)
                    {
                        string tvResourceString = client.DownloadString(tvSearch + tvShow.Name);
                        Console.WriteLine(tvResourceString);

                        JObject tvObject = JObject.Parse(tvResourceString);
                        int totalResults = (int)tvObject["total_results"];

                        if (totalResults == 0)
                        {
                            CustomMessageForm("Error", "No tv show for: " + tvShow.Name);
                        }
                        else if (totalResults != 1)
                        {
                            int actualResults = (int)((JArray)tvObject["results"]).Count();
                            string[] names = new string[actualResults];
                            string[] ids = new string[actualResults];
                            string[] overviews = new string[actualResults];
                            DateTime?[] dates = new DateTime?[actualResults];

                            for (int j = 0; j < actualResults; j++)
                            {
                                names[j] = (string)tvObject["results"][j]["name"];
                                ids[j] = (string)tvObject["results"][j]["id"];
                                overviews[j] = (string)tvObject["results"][j]["overview"];
                                DateTime temp;
                                dates[j] = DateTime.TryParse((string)tvObject["results"][j]["first_air_date"], out temp) ? temp : DateTime.MinValue.AddHours(9);
                            }
                            string[][] info = new string[][] { names, ids, overviews };
                            tvShow.Id = ShowOptionsForm(tvShow.Name, info, dates);
                        }
                        else
                        {
                            tvShow.Id = (int)tvObject["results"][0]["id"];
                        }

                        string tvString = client.DownloadString(tvGet.Replace("{tv_id}", tvShow.Id.ToString()));
                        Console.WriteLine(tvString);
                        tvObject = JObject.Parse(tvString);
                        tvShow.Overview = (string)tvObject["overview"];
                        tvShow.Poster = (string)tvObject["poster_path"];
                        tvShow.Backdrop = (string)tvObject["backdrop_path"];
                        DateTime tempDate;
                        tvShow.Date = DateTime.TryParse((string)tvObject["first_air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                        if (tvShow.Backdrop != null)
                        {
                            await SaveImage(tvShow.Backdrop, tvShow.Name, false, token);
                            tvShow.Backdrop = bufferString;
                        }
                        if (tvShow.Poster != null)
                        {
                            await SaveImage(tvShow.Poster, tvShow.Name, false, token);
                            tvShow.Poster = bufferString;
                        }
                    }

                    Console.WriteLine("Show: " + tvShow.Name);
                    int seasonIndex = 0;
                    for (int j = 0; j < tvShow.Seasons.Length; j++)
                    {
                        Season season = tvShow.Seasons[j];
                        Console.WriteLine("season" + " " + j);
                        string seasonApiCall = tvSeasonGet.Replace("{tv_id}", tvShow.Id.ToString()).Replace("{season_number}", seasonIndex.ToString());
                        Console.WriteLine(seasonApiCall);
                        string seasonString = client.DownloadString(seasonApiCall);
                        Console.WriteLine(seasonString);
                        JObject seasonObject = JObject.Parse(seasonString);

                        if (!((string)seasonObject["name"]).Contains("Season"))
                        {
                            Console.WriteLine("Season 0 was Specials");
                            seasonIndex++;
                            seasonString = client.DownloadString(tvSeasonGet.Replace("{tv_id}", tvShow.Id.ToString()).Replace("{season_number}", seasonIndex.ToString()));
                            Console.WriteLine(seasonString);
                            seasonObject = JObject.Parse(seasonString);
                        }

                        //what if poster is null
                        if (season.Poster == null)
                        {
                            season.Poster = (string)seasonObject["poster_path"];
                            DateTime tempDate;
                            season.Date = DateTime.TryParse((string)seasonObject["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                            if (season.Poster != null)
                            {
                                await SaveImage(season.Poster, tvShow.Name, false, token);
                                season.Poster = bufferString;
                            }
                        }

                        JArray jEpisodes = (JArray)seasonObject["episodes"];
                        Episode[] episodes = season.Episodes;

                        for (int k = 0; k < episodes.Length; k++)
                        {
                            //To-do: what if backdrop is null
                            Console.WriteLine(k + " " + episodes[k].Name);
                            if (episodes[k].Backdrop != null) continue;

                            JObject jEpisode = (JObject)jEpisodes[k];
                            Episode episode = episodes[k];

                            if (String.Compare(episode.Name, (string)jEpisode["name"], System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0
                                && episode.Id == (int)jEpisode["episode_number"])
                            {
                                episode.Id = (int)jEpisode["episode_number"];
                                episode.Overview = (string)jEpisode["overview"];
                                episode.Backdrop = (string)jEpisode["still_path"];
                                DateTime tempDate;
                                episode.Date = DateTime.TryParse((string)jEpisode["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                                //To-do: if season does not match? if one or the other matches error checking
                                if (episode.Backdrop != null)
                                {
                                    await SaveImage(episode.Backdrop, tvShow.Name, false, token);
                                    episode.Backdrop = bufferString;
                                }
                            }
                            else
                            {
                                string message = "Local episode name does not match retrieved data. Renaming file '" + episode.Name + "' to '" + (string)jEpisode["name"] + "'.";
                                CustomMessageForm("Warning", message);

                                string oldPath = episode.Path;
                                string newPath = oldPath.Replace(episode.Name, (string)jEpisode["name"]);
                                File.Move(oldPath, newPath);

                                episode.Path = newPath;
                                episode.Name = (string)jEpisode["name"];
                                episode.Id = (int)jEpisode["episode_number"];
                                episode.Overview = (string)jEpisode["overview"];
                                episode.Backdrop = (string)jEpisode["still_path"];
                                DateTime tempDate;
                                episode.Date = DateTime.TryParse((string)jEpisode["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                                //To-do: separate function
                                if (episode.Backdrop != null)
                                {
                                    await SaveImage(episode.Backdrop, tvShow.Name, false, token);
                                    episode.Backdrop = bufferString;
                                }
                            }
                        }
                        seasonIndex++; //can be removed..?
                    }
                }
            }

            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);
        }

        private async Task SaveImage(string imagePath, string name, bool isMovie, CancellationToken token)
        {
            string url = apiImageUrl + imagePath;
            string dirPath;
            string filePath;
            if (isMovie)
            {
                dirPath = "Cache\\Movies\\" + name;
                filePath = dirPath + imagePath.Replace("/", "\\");
            }
            else
            {
                dirPath = "Cache\\TV\\" + name;
                filePath = dirPath + imagePath.Replace("/", "\\");
            }
            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, short.MaxValue, true))
                {
                    try
                    {
                        var requestUri = new Uri(url);
                        HttpClientHandler handler = new HttpClientHandler
                        {
                            PreAuthenticate = true,
                            UseDefaultCredentials = true
                        };
                        var response = await (new HttpClient(handler)).GetAsync(requestUri,
                            HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                        var content = response.EnsureSuccessStatusCode().Content;
                        await content.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError(ex.ToString());
                    }
                }
            }
            bufferString = filePath;
        }

        private bool CheckForUpdates()
        {
            Media prevMedia = null;
            if (File.Exists(jsonFile))
            {
                string jsonString = File.ReadAllText(jsonFile);
                prevMedia = JsonConvert.DeserializeObject<Media>(jsonString);
            }

            if (prevMedia == null)
            {
                Console.WriteLine("First start");
                return true;
            }

            bool result = !media.Compare(prevMedia);
            if (!result)
            {
                media = prevMedia;
            }
            else
            {
                media.Ingest(prevMedia);
            }

            return result;
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
                media.TvShows[i] = ProcessTvDirectory(tvEntries[i]);
            }

        }

        private Movie ProcessMovieDirectory(string targetDir)
        {
            Console.WriteLine("    " + targetDir);
            string[] movieEntry = Directory.GetFiles(targetDir);
            string[] path = movieEntry[0].Split('\\');
            string[] movieName = path[path.Length - 1].Split('.');
            Movie movie = new Movie(movieName[0].Trim(), movieEntry[0]);
            return movie;
        }

        private TvShow ProcessTvDirectory(string targetDir)
        {
            Console.WriteLine("    " + targetDir);
            string[] path = targetDir.Split('\\');
            string name = path[path.Length - 1].Split('%')[0];
            TvShow show = new TvShow(name.Trim());
            string[] seasonEntries = Directory.GetDirectories(targetDir);
            show.Seasons = new Season[seasonEntries.Length];
            for (int i = 0; i < seasonEntries.Length; i++)
            {
                Season season = new Season(i + 1);
                string[] episodeEntries = Directory.GetFiles(seasonEntries[i]);
                season.Episodes = new Episode[episodeEntries.Length];
                for (int j = 0; j < episodeEntries.Length; j++)
                {
                    string[] namePath = episodeEntries[j].Split('\\');
                    string[] episodeNameNumber = namePath[namePath.Length - 1].Split('%');
                    string episodeName = episodeNameNumber[1].Split('.')[0].Trim();
                    Episode episode = new Episode(Int32.Parse(episodeNameNumber[0].Trim()), episodeName, episodeEntries[j]);
                    Console.WriteLine("        " + episode.Name);
                    season.Episodes[j] = episode;
                }
                show.Seasons[i] = season;
            }
            return show;
        }

        private void headerLabel_Paint(object sender, PaintEventArgs e)
        {
            float fontSize = NewHeaderFontSize(e.Graphics, this.Bounds.Size, movieLabel.Font, movieLabel.Text);
            Font f = new Font("Arial", fontSize, FontStyle.Bold);
            movieLabel.Font = f;
            tvLabel.Font = f;
        }

        public static float NewHeaderFontSize(Graphics graphics, Size size, Font font, string str)
        {
            SizeF stringSize = graphics.MeasureString(str, font);
            float ratio = (size.Height / stringSize.Height) / 10;
            return font.Size * ratio;
        }
    }
}
