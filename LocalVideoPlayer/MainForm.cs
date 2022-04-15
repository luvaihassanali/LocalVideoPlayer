using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomControls;
using LocalVideoPlayer.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalVideoPlayer
{
    public partial class MainForm : Form
    {
        #region Dll import

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);
        private const int SPI_SETCURSORS = 0x0057;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;
        [DllImport("user32.dll")]

        static public extern bool ShowScrollBar(System.IntPtr hWnd, int wBar, bool bShow);

        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)] public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)] public UInt32 dwTime;
        }

        #endregion

        #region TMDB API

        private const string jsonFile = "Media.json";
        private const string apiKey = "?api_key=c69c4effc7beb9c473d22b8f85d59e4c";
        private const string apiUrl = "https://api.themoviedb.org/3/";
        private const string apiImageUrl = "http://image.tmdb.org/t/p/original";
        private const string tvSearch = apiUrl + "search/tv" + apiKey + "&query=";
        private const string movieSearch = apiUrl + "search/movie" + apiKey + "&query=";
        private string tvGet = apiUrl + "tv/{tv_id}" + apiKey;
        private string tvSeasonGet = apiUrl + "tv/{tv_id}/season/{season_number}" + apiKey;
        private string movieGet = apiUrl + "movie/{movie_id}" + apiKey;
        private string bufferString = "";

        #endregion

        static public Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);
        static public Form dimmerForm;
        static public Form seasonDimmerForm;
        static public LayoutController layoutController;
        static public MediaModel media;
        static public Point mainFormLoc;
        static public Size mainFormSize;
        static private bool debugLog;
        static private string debugLogPath;

        private bool mouseMoverClientKill = false;
        private CustomScrollbar customScrollbar = null;
        private Label movieLabel;
        private Label tvLabel;
        private MouseWorker worker = null;
        private System.Threading.Timer idleMainFormTimer = null;
        private System.Threading.Timer idlePauseFormTimer = null;
        private Panel mainFormMainPanel = null;

        public MainForm()
        {
            InitializeMainFormComponents();
            InitializeCustomCursor();
            InitializeDimmers();
            InitializeMouseWorker();
#if DEBUG
            this.WindowState = FormWindowState.Normal;
#endif

            closeButton.Click += new EventHandler(CloseButton_Click);

            backgroundWorker1.DoWork += new DoWorkEventHandler(BackgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.RunWorkerAsync();
        }

        #region General form functions

        private void MainForm_Load(object sender, EventArgs e)
        {
            loadingCircle1.Location = new Point(this.Width / 2 - loadingCircle1.Width / 2, this.Height / 2 - loadingCircle1.Height / 2);
            loadingLabel.Location = new Point(0, this.Height / 2 - loadingLabel.Height / 2 + 2);
            loadingLabel.Size = new Size(this.Width, loadingLabel.Height);
            Log("Application start");
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            mainFormSize = this.Size;
            mainFormLoc = this.Location;
            this.Focus();
            this.Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (media != null)
                {
                    SaveMedia();
                }

                RestoreSystemCursor();

                if (worker != null)
                {
                    worker.StopImmediately();
                    worker = null;
                }

                if (mouseMoverClientKill)
                {
                    string mouseMoverPath = ConfigurationManager.AppSettings["mouseMoverPath"];
                    Process.Start(mouseMoverPath);
                }

                if (idlePauseFormTimer != null)
                {
                    idlePauseFormTimer.Dispose();
                }

                if (idleMainFormTimer != null)
                {
                    idleMainFormTimer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log("Application end exception: " + ex.Message);
            }
            Log("Application end");
            for (int i = 0; i < 2; i++) Log(Environment.NewLine);
        }

        static public void SaveMedia()
        {
            Log("Saving media...");
            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);
            Log("Media saved");
        }

        static public void CloseButton_Click(object sender, EventArgs e)
        {
            Button closeButton = sender as Button;
            Panel mainPanel = closeButton.Parent as Panel;
            Form currentForm = mainPanel != null ? (Form)mainPanel.Parent : (Form)closeButton.Parent;

            currentForm.Close();
            if (!currentForm.Name.Equals("MainForm"))
            {
                Fader.FadeOut(dimmerForm, Fader.FadeSpeed.Normal);
            }
        }

        private void CustomScrollbar_Scroll(object sender, EventArgs e)
        {
            mainFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
            closeButton.Visible = customScrollbar.Value == 0 ? true : false;
        }

        private void MainFormMainPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            int newVal = -mainFormMainPanel.AutoScrollPosition.Y;
            closeButton.Visible = newVal == 0 ? true : false;
            customScrollbar.Value = newVal;
        }

        private void HeaderLabel_Paint(object sender, PaintEventArgs e)
        {
            float fontSize = GetHeaderFontSize(e.Graphics, this.Bounds.Size, movieLabel.Font, movieLabel.Text);
            Font f = new Font("Arial", fontSize, FontStyle.Bold);
            movieLabel.Font = f;
            tvLabel.Font = f;
        }

        public static float GetHeaderFontSize(Graphics graphics, Size size, Font font, string str)
        {
            SizeF stringSize = graphics.MeasureString(str, font);
            float ratio = (size.Height / stringSize.Height) / 10;
            return font.Size * ratio;
        }

        #endregion

        #region Startup

        private void InitializeGui()
        {
            mainFormMainPanel = new Panel();
            mainFormMainPanel.Size = this.Size;
            mainFormMainPanel.AutoScroll = true;
            mainFormMainPanel.Name = "mainFormMainPanel";
            mainFormMainPanel.MouseWheel += MainFormMainPanel_MouseWheel;
            layoutController.mainFormMainPanel = mainFormMainPanel;

            closeButton.Visible = true;
            closeButton.Location = new Point(mainFormMainPanel.Width - (int)(closeButton.Width * 1.75), (closeButton.Width / 6));
            closeButton.Cursor = blueHandCursor;
            layoutController.mainFormClose = closeButton;

            Panel currentPanel = null;
            int count = 0;
            int panelCount = 0;
            int widthValue = (int)(mainFormMainPanel.Width / 6.12);
            int heightValue = (int)(widthValue * 1.5);

            for (int i = 0; i < media.Movies.Length; i++)
            {
                if (count == 6) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Black;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "movie" + panelCount;
                    panelCount++;
                    mainFormMainPanel.Controls.Add(currentPanel);
                    mainFormMainPanel.Controls.SetChildIndex(currentPanel, 0);
                }

                PictureBox movieBox = new PictureBox();
                movieBox.Width = widthValue;
                movieBox.Height = heightValue;
                string imagePath = media.Movies[i].Poster;
                try
                {
                    movieBox.Image = Image.FromFile(imagePath);
                }
                catch
                {
                    movieBox.Image = Properties.Resources.noprev;
                }
                movieBox.BackColor = Color.Black;
                movieBox.Left = movieBox.Width * currentPanel.Controls.Count;
                movieBox.Cursor = blueHandCursor;
                movieBox.SizeMode = PictureBoxSizeMode.StretchImage;
                movieBox.Padding = new Padding(20);
                movieBox.Name = media.Movies[i].Name;
                movieBox.Click += TvForm.MovieBox_Click;
                movieBox.MouseEnter += (s, e) =>
                {
                    layoutController.ClearMovieBoxBorder();
                    movieBox.BorderStyle = BorderStyle.Fixed3D;
                };
                movieBox.MouseLeave += (s, e) =>
                {
                    movieBox.BorderStyle = BorderStyle.None;
                };
                currentPanel.Controls.Add(movieBox);
                layoutController.mainFormControlList.Add(movieBox);
                layoutController.movieBoxes.Add(movieBox);
                count++;
            }

            movieLabel = new Label();
            movieLabel.Text = "Movies";
            movieLabel.Dock = DockStyle.Top;
            movieLabel.Paint += HeaderLabel_Paint;
            movieLabel.AutoSize = true;
            movieLabel.Name = "movieLabel";
            mainFormMainPanel.Controls.Add(movieLabel);

            currentPanel = null;
            count = 0;
            panelCount = 0;

            for (int i = 0; i < media.TvShows.Length; i++)
            {
                if (count == 6) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Black;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "tv" + panelCount;
                    panelCount++;
                    mainFormMainPanel.Controls.Add(currentPanel);
                    mainFormMainPanel.Controls.SetChildIndex(currentPanel, 3);
                }

                PictureBox tvShowBox = new PictureBox();

                tvShowBox.Width = widthValue;
                tvShowBox.Height = heightValue;
                string imagePath = media.TvShows[i].Poster;
                try
                {
                    tvShowBox.Image = Image.FromFile(imagePath);
                }
                catch
                {
                    tvShowBox.Image = Properties.Resources.noprev;
                }
                tvShowBox.BackColor = Color.Black;
                tvShowBox.Left = tvShowBox.Width * currentPanel.Controls.Count;
                tvShowBox.Cursor = blueHandCursor;
                tvShowBox.SizeMode = PictureBoxSizeMode.StretchImage;
                tvShowBox.Padding = new Padding(20);
                tvShowBox.Name = media.TvShows[i].Name;
                tvShowBox.Click += TvForm.TvShowBox_Click;
                tvShowBox.MouseEnter += (s, e) =>
                {
                    layoutController.ClearTvBoxBorder();
                    tvShowBox.BorderStyle = BorderStyle.Fixed3D;
                };
                tvShowBox.MouseLeave += (s, e) =>
                {
                    tvShowBox.BorderStyle = BorderStyle.None;
                };
                currentPanel.Controls.Add(tvShowBox);
                layoutController.mainFormControlList.Add(tvShowBox);
                layoutController.tvBoxes.Add(tvShowBox);
                count++;
            }

            tvLabel = new Label();
            tvLabel.Text = "TV Shows";
            tvLabel.Dock = DockStyle.Top;
            tvLabel.Paint += HeaderLabel_Paint;
            tvLabel.AutoSize = true;
            tvLabel.Name = "tvLabel";

            mainFormMainPanel.Controls.Add(tvLabel);
            this.Controls.Add(mainFormMainPanel);
            customScrollbar = CustomDialog.CreateScrollBar(mainFormMainPanel);
            customScrollbar.Scroll += CustomScrollbar_Scroll;
            this.Controls.Add(customScrollbar);
            customScrollbar.BringToFront();
            layoutController.mainScrollbar = customScrollbar;
        }

        private bool CheckForUpdates()
        {
            MediaModel prevMedia = null;

            if (File.Exists(jsonFile))
            {
                string jsonString = File.ReadAllText(jsonFile);
                prevMedia = JsonConvert.DeserializeObject<MediaModel>(jsonString);
            }

            if (prevMedia == null)
            {
                return true;
            }

            bool result = !media.Compare(prevMedia);

            if (!result)
            {
                media = prevMedia;
            }
            else
            {
                File.Copy(jsonFile, jsonFile + ".bak", true);
                media.Ingest(prevMedia);
            }

            return result;
        }

        private void InitializeDimmers()
        {
            dimmerForm = new Form();
            dimmerForm.ShowInTaskbar = false;
            dimmerForm.FormBorderStyle = FormBorderStyle.None;
            dimmerForm.BackColor = Color.Black;

            seasonDimmerForm = new Form();
            seasonDimmerForm.ShowInTaskbar = false;
            seasonDimmerForm.FormBorderStyle = FormBorderStyle.None;
            seasonDimmerForm.BackColor = Color.Black;
        }

        private void InitializeMouseWorker()
        {
            Process applicaitionProcess = Process.GetCurrentProcess();
            applicaitionProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

            Process[] p = Process.GetProcessesByName("MouseMoverClient");
            if (p.Length != 0)
            {
                Process.GetProcessesByName("MouseMoverClient")[0].Kill();
                mouseMoverClientKill = true;
            }

            debugLogPath = ConfigurationManager.AppSettings["debugLogPath"] + "lvp-debug.log";
            debugLog = bool.Parse(ConfigurationManager.AppSettings["debugLog"]);

            worker = new MouseWorker(this);
            worker.Start();
        }

        #endregion

        #region Build cache

        private void UpdateLoadingLabel(string text)
        {
            bool bringToFront = false;
            if (text == null)
            {
                bringToFront = true;
            }

            loadingLabel.Invoke(new MethodInvoker(delegate
            {
                loadingLabel.Text = text;
                if (bringToFront)
                {
                    loadingLabel.BringToFront();
                }
            }));
        }

        private async Task BuildCacheAsync()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // Loop through media... check for identifying item only from api...if not there update
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                for (int i = 0; i < media.Movies.Length; i++)
                {
                    // If id is not 0 expected to be init
                    if (media.Movies[i].Id != 0) continue;
                    UpdateLoadingLabel("Processing: " + media.Movies[i].Name);
                    Movie movie = media.Movies[i];
                    string movieResourceString = client.DownloadString(movieSearch + movie.Name);

                    JObject movieObject = JObject.Parse(movieResourceString);
                    int totalResults = (int)movieObject["total_results"];

                    if (totalResults == 0)
                    {
                        CustomDialog.ShowMessage("Error", "No movie found for: " + movie.Name, this.Width, this.Height);
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
                            names[j] = names[j].fixBrokenQuotes();
                            ids[j] = (string)movieObject["results"][j]["id"];
                            overviews[j] = (string)movieObject["results"][j]["overview"];
                            overviews[j] = overviews[j].fixBrokenQuotes();
                            DateTime temp;
                            dates[j] = DateTime.TryParse((string)movieObject["results"][j]["release_date"], out temp) ? temp : DateTime.MinValue.AddHours(9);
                        }

                        string[][] info = new string[][] { names, ids, overviews };
                        movie.Id = CustomDialog.ShowOptions(movie.Name, info, dates, this.Width, this.Height);
                    }
                    else
                    {
                        movie.Id = (int)movieObject["results"][0]["id"];
                    }
                    //To-do: 404 not found
                    string movieString = client.DownloadString(movieGet.Replace("{movie_id}", movie.Id.ToString()));
                    movieObject = JObject.Parse(movieString);

                    if (String.Compare(movie.Name.Replace(":", ""), ((string)movieObject["title"]).Replace(":", "").fixBrokenQuotes(), System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
                    {
                        movie.Backdrop = (string)movieObject["backdrop_path"];
                        movie.Poster = (string)movieObject["poster_path"];
                        movie.Overview = (string)movieObject["overview"];
                        movie.Overview = movie.Overview.fixBrokenQuotes();
                        movie.RunningTime = (int)movieObject["runtime"];

                        DateTime tempDate;
                        movie.Date = DateTime.TryParse((string)movieObject["release_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                        if (movie.Backdrop != null)
                        {
                            await DownloadImage(movie.Backdrop, movie.Name, true, token);
                            movie.Backdrop = bufferString;
                        }

                        if (movie.Poster != null)
                        {
                            await DownloadImage(movie.Poster, movie.Name, true, token);
                            movie.Poster = bufferString;
                        }
                    }
                    else
                    {
                        string message = "Local movie name does not match retrieved data. Renaming file '" + movie.Name.Replace(":", "") + "' to '" + ((string)movieObject["title"]).Replace(":", "") + "'.";
                        CustomDialog.ShowMessage("Warning", message, this.Width, this.Height);

                        string oldPath = movie.Path;
                        string[] fileNamePath = oldPath.Split('\\');
                        string fileName = fileNamePath[fileNamePath.Length - 1];
                        string extension = fileName.Split('.')[1];
                        string newFileName = ((string)movieObject["title"]).Replace(":", "").fixBrokenQuotes(); ;
                        string newPath = oldPath.Replace(fileName, newFileName + "." + extension);
                        string invalid = new string(Path.GetInvalidPathChars()) + '?';

                        foreach (char c in invalid)
                        {
                            newPath = newPath.Replace(c.ToString(), "");
                        }

                        File.Move(oldPath, newPath);

                        movie.Path = newPath;
                        movie.Name = newFileName;
                        movie.Id = (int)movieObject["id"];
                        movie.Backdrop = (string)movieObject["backdrop_path"];
                        movie.Poster = (string)movieObject["poster_path"];
                        movie.Overview = (string)movieObject["overview"];
                        movie.Overview = movie.Overview.fixBrokenQuotes();

                        DateTime tempDate;
                        movie.Date = DateTime.TryParse((string)movieObject["release_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                        if (movie.Backdrop != null)
                        {
                            await DownloadImage(movie.Backdrop, movie.Name, true, token);
                            movie.Backdrop = bufferString;
                        }

                        if (movie.Poster != null)
                        {
                            await DownloadImage(movie.Poster, movie.Name, true, token);
                            movie.Poster = bufferString;
                        }
                    }
                }

                for (int i = 0; i < media.TvShows.Length; i++)
                {
                    TvShow tvShow = media.TvShows[i];

                    // If id is not 0 then general show data initialized
                    if (tvShow.Id == 0)
                    {
                        string tvResourceString = client.DownloadString(tvSearch + tvShow.Name);

                        JObject tvObject = JObject.Parse(tvResourceString);
                        int totalResults = (int)tvObject["total_results"];

                        if (totalResults == 0)
                        {
                            CustomDialog.ShowMessage("Error", "No tv show for: " + tvShow.Name, this.Width, this.Height);
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
                                names[j] = names[j].fixBrokenQuotes();
                                ids[j] = (string)tvObject["results"][j]["id"];
                                overviews[j] = (string)tvObject["results"][j]["overview"];
                                overviews[j] = overviews[j].fixBrokenQuotes();

                                DateTime temp;
                                dates[j] = DateTime.TryParse((string)tvObject["results"][j]["first_air_date"], out temp) ? temp : DateTime.MinValue.AddHours(9);
                            }

                            string[][] info = new string[][] { names, ids, overviews };
                            tvShow.Id = CustomDialog.ShowOptions(tvShow.Name, info, dates, this.Width, this.Height);
                        }
                        else
                        {
                            tvShow.Id = (int)tvObject["results"][0]["id"];
                        }

                        string tvString = client.DownloadString(tvGet.Replace("{tv_id}", tvShow.Id.ToString()));
                        tvObject = JObject.Parse(tvString);
                        tvShow.Overview = (string)tvObject["overview"];
                        tvShow.Overview = tvShow.Overview.fixBrokenQuotes();
                        tvShow.Poster = (string)tvObject["poster_path"];
                        tvShow.Backdrop = (string)tvObject["backdrop_path"];
                        tvShow.RunningTime = (int)tvObject["episode_run_time"][0];

                        DateTime tempDate;
                        tvShow.Date = DateTime.TryParse((string)tvObject["first_air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                        if (tvShow.Backdrop != null)
                        {
                            await DownloadImage(tvShow.Backdrop, tvShow.Name, false, token);
                            tvShow.Backdrop = bufferString;
                        }

                        if (tvShow.Poster != null)
                        {
                            await DownloadImage(tvShow.Poster, tvShow.Name, false, token);
                            tvShow.Poster = bufferString;
                        }
                    }

                    // Always check season data for new content
                    int seasonIndex = 0;
                    for (int j = 0; j < tvShow.Seasons.Length; j++)
                    {
                        Season season = tvShow.Seasons[j];

                        if (season.Id == -1) continue;

                        string seasonLabel = tvShow.Seasons[j].Id == -1 ? "Extras" : (j + 1).ToString();
                        UpdateLoadingLabel("Processing: " + tvShow.Name + " Season " + seasonLabel);

                        string seasonApiCall = tvSeasonGet.Replace("{tv_id}", tvShow.Id.ToString()).Replace("{season_number}", seasonIndex.ToString());
                        string seasonString = client.DownloadString(seasonApiCall);
                        JObject seasonObject = JObject.Parse(seasonString);

                        if (((string)seasonObject["name"]).Contains("Specials"))
                        {
                            seasonIndex++;
                            seasonString = client.DownloadString(tvSeasonGet.Replace("{tv_id}", tvShow.Id.ToString()).Replace("{season_number}", seasonIndex.ToString()));
                            seasonObject = JObject.Parse(seasonString);
                        }

                        if (season.Poster == null)
                        {
                            season.Poster = (string)seasonObject["poster_path"];
                            DateTime tempDate;
                            season.Date = DateTime.TryParse((string)seasonObject["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                            if (season.Poster != null)
                            {
                                await DownloadImage(season.Poster, tvShow.Name, false, token);
                                season.Poster = bufferString;
                            }
                        }

                        JArray jEpisodes = (JArray)seasonObject["episodes"];
                        Episode[] episodes = season.Episodes;
                        int jEpIndex = 0;
                        for (int k = 0; k < episodes.Length; k++)
                        {
                            if (episodes[k].Id != 0)
                            {
                                jEpIndex++;
                                continue;
                            }
                            if (k > jEpisodes.Count - 1)
                            {
                                continue;
                            }
                            Episode episode = episodes[k];

                            if (episode.Name.Contains('#'))
                            {
                                string[] multiEpNames = episode.Name.Split('#');
                                JObject[] jEpisodesMulti = new JObject[multiEpNames.Length];
                                int numEps = multiEpNames.Length;
                                String multiEpisodeOverview = "";
                                for (int l = 0; l < numEps; l++)
                                {
                                    jEpisodesMulti[l] = (JObject)jEpisodes[jEpIndex + l];
                                    String jCurrMultiEpisodeName = (string)jEpisodesMulti[l]["name"];
                                    String jCurrMultiEpisodeOverview = (string)jEpisodesMulti[l]["overview"];
                                    String currMultiEpisodeName = multiEpNames[l];
                                    if (String.Compare(currMultiEpisodeName, jCurrMultiEpisodeName.fixBrokenQuotes(), System.Globalization.CultureInfo.CurrentCulture,
                                        System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) != 0)
                                    {
                                        string message = "Multi episode name does not match retrieved data: Episode name: '" + currMultiEpisodeName + ", retrieved: " + jCurrMultiEpisodeName.fixBrokenQuotes() + " (Season " + season.Id + ").";
                                        CustomDialog.ShowMessage("Warning: " + tvShow.Name, message, this.Width, this.Height);
                                    }
                                    multiEpisodeOverview += (jCurrMultiEpisodeOverview + Environment.NewLine + Environment.NewLine);
                                }

                                episode.Id = (int)jEpisodesMulti[numEps - 1]["episode_number"];
                                episode.Backdrop = (string)jEpisodesMulti[numEps - 1]["still_path"];
                                episode.Overview = multiEpisodeOverview;
                                DateTime tempDate;
                                episode.Date = DateTime.TryParse((string)jEpisodesMulti[numEps - 1]["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                                if (episode.Backdrop != null)
                                {
                                    await DownloadImage(episode.Backdrop, tvShow.Name, false, token);
                                    episode.Backdrop = bufferString;
                                }
                                jEpIndex += (numEps);
                                continue;
                            }

                            JObject jEpisode = (JObject)jEpisodes[jEpIndex];
                            String jEpisodeName = (string)jEpisode["name"];
                            if (String.Compare(episode.Name, jEpisodeName.fixBrokenQuotes(),
                                System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
                            {
                                episode.Id = (int)jEpisode["episode_number"];
                                episode.Overview = (string)jEpisode["overview"];
                                episode.Overview = episode.Overview.fixBrokenQuotes();
                                episode.Backdrop = (string)jEpisode["still_path"];
                                DateTime tempDate;
                                episode.Date = DateTime.TryParse((string)jEpisode["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                                if (episode.Backdrop != null)
                                {
                                    await DownloadImage(episode.Backdrop, tvShow.Name, false, token);
                                    episode.Backdrop = bufferString;
                                }
                            }
                            else
                            {
                                string message = "Local episode name for does not match retrieved data. Renaming file '" + episode.Name + "' to '" + jEpisodeName.fixBrokenQuotes() + "' (Season " + season.Id + ").";
                                CustomDialog.ShowMessage("Warning: " + tvShow.Name, message, this.Width, this.Height);

                                string oldPath = episode.Path;
                                jEpisodeName = (string)jEpisode["name"];
                                string newPath = oldPath.Replace(episode.Name, jEpisodeName.fixBrokenQuotes());
                                string invalid = new string(Path.GetInvalidPathChars()) + '?' + ':';
                                foreach (char c in invalid)
                                {
                                    newPath = newPath.Replace(c.ToString(), "");
                                }
                                try
                                {
                                    char drive = newPath[0];
                                    string drivePath = drive + ":";
                                    newPath = ReplaceFirst(newPath, drive.ToString(), drivePath);

                                    File.Move(oldPath, newPath);
                                }
                                catch (Exception e)
                                {
                                    CustomDialog.ShowMessage("Error", e.Message, this.Width, this.Height);
                                }

                                episode.Path = newPath;
                                episode.Name = jEpisodeName.fixBrokenQuotes();
                                episode.Id = (int)jEpisode["episode_number"];
                                episode.Overview = (string)jEpisode["overview"];
                                episode.Overview = episode.Overview.fixBrokenQuotes();
                                episode.Backdrop = (string)jEpisode["still_path"];
                                DateTime tempDate;
                                episode.Date = DateTime.TryParse((string)jEpisode["air_date"], out tempDate) ? tempDate : DateTime.MinValue.AddHours(9);

                                if (episode.Backdrop != null)
                                {
                                    await DownloadImage(episode.Backdrop, tvShow.Name, false, token);
                                    episode.Backdrop = bufferString;
                                }
                            }
                            jEpIndex++;
                        }
                        seasonIndex++;
                    }
                }
            }

            Array.Sort(media.Movies, Movie.SortMoviesAlphabetically());
            Array.Sort(media.TvShows, TvShow.SortTvShowsAlphabetically());

            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private async Task DownloadImage(string imagePath, string name, bool isMovie, CancellationToken token)
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

        #endregion  

        #region Process directory

        private void ProcessDirectory(string targetDir, string targetDirB)
        {
            string[] moviesDir = new string[2];
            string[] tvDir = new string[2];
            string[] subdirectoryEntries;
            string[] subdirectoryEntriesB = null;
            bool subdirectoryBExists = false;
            try
            {
                subdirectoryEntries = Directory.GetDirectories(targetDir);
                if (targetDirB != String.Empty)
                {
                    subdirectoryEntriesB = Directory.GetDirectories(targetDirB);
                    subdirectoryBExists = true;
                }
            }
            catch
            {
                Log("Missing sub directories");
                throw new ArgumentNullException();
            }

            foreach (string subDir in subdirectoryEntries)
            {
                string[] subDirPath = subDir.Split('\\');
                string targetSubDir = subDirPath[subDirPath.Length - 1].ToLower();
                if (targetSubDir.ToLower().Equals("movies"))
                {
                    moviesDir[0] = subDir;
                }
                if (targetSubDir.Equals("tv"))
                {
                    tvDir[0] = subDir;
                }
            }

            if (subdirectoryBExists)
            {
                foreach (string subDir in subdirectoryEntriesB)
                {
                    string[] subDirPath = subDir.Split('\\');
                    string targetSubDir = subDirPath[subDirPath.Length - 1].ToLower();
                    if (targetSubDir.ToLower().Equals("movies"))
                    {
                        moviesDir[1] = subDir;
                    }
                    if (targetSubDir.Equals("tv"))
                    {
                        tvDir[1] = subDir;
                    }
                }
            }

            if (moviesDir == null || tvDir == null)
            {
                Log("Missing sub directories");
                throw new ArgumentNullException();
            }

            int moviesCount = subdirectoryBExists ? Directory.GetDirectories(moviesDir[0]).Length + Directory.GetDirectories(moviesDir[1]).Length : Directory.GetDirectories(moviesDir[0]).Length;
            int tvCount = subdirectoryBExists ? Directory.GetDirectories(tvDir[0]).Length + Directory.GetDirectories(tvDir[1]).Length : Directory.GetDirectories(tvDir[0]).Length;

            media = new MediaModel(moviesCount, tvCount);
            string[] movieEntries = Directory.GetDirectories(moviesDir[0]);
            for (int i = 0; i < movieEntries.Length; i++)
            {

                media.Movies[i] = ProcessMovieDirectory(movieEntries[i]);
            }

            string[] tvEntries = Directory.GetDirectories(tvDir[0]);
            for (int i = 0; i < tvEntries.Length; i++)
            {
                media.TvShows[i] = ProcessTvDirectory(tvEntries[i]);
            }

            if (subdirectoryBExists)
            {
                int index = 0;
                string[] movieEntriesB = Directory.GetDirectories(moviesDir[1]);
                for (int i = movieEntries.Length; i < moviesCount; i++)
                {

                    media.Movies[i] = ProcessMovieDirectory(movieEntriesB[index++]);
                }

                string[] tvEntriesB = Directory.GetDirectories(tvDir[1]);
                index = 0;
                for (int i = tvEntries.Length; i < tvCount; i++)
                {
                    media.TvShows[i] = ProcessTvDirectory(tvEntriesB[index++]);
                }
            }

        }

        private Movie ProcessMovieDirectory(string targetDir)
        {
            string[] movieEntry = Directory.GetFiles(targetDir);
            string[] path = movieEntry[0].Split('\\');
            string[] movieName = path[path.Length - 1].Split('.');
            Movie movie = new Movie(movieName[0].Trim(), movieEntry[0]);
            return movie;
        }

        private TvShow ProcessTvDirectory(string targetDir)
        {
            string[] path = targetDir.Split('\\');
            string name = path[path.Length - 1].Split('%')[0];
            TvShow show = new TvShow(name.Trim());
            string[] seasonEntries = Directory.GetDirectories(targetDir);
            Array.Sort(seasonEntries, SeasonComparer);
            show.Seasons = new Season[seasonEntries.Length];
            for (int i = 0; i < seasonEntries.Length; i++)
            {
                if (seasonEntries[i].Contains("Extras"))
                {
                    Season extras = new Season(-1);
                    List<Episode> extraEpisodes = new List<Episode>();
                    ProcessExtrasDirectory(extraEpisodes, seasonEntries[i]);
                    extras.Episodes = new Episode[extraEpisodes.Count];
                    for (int j = 0; j < extraEpisodes.Count; j++)
                    {
                        extras.Episodes[j] = extraEpisodes[j];
                    }
                    show.Seasons[show.Seasons.Length - 1] = extras;
                    continue;
                }

                if (!seasonEntries[i].Contains("Season")) continue;

                Season season = new Season(i + 1);
                string[] episodeEntries = Directory.GetFiles(seasonEntries[i]);
                Array.Sort(episodeEntries);
                season.Episodes = new Episode[episodeEntries.Length];
                for (int j = 0; j < episodeEntries.Length; j++)
                {
                    string[] namePath = episodeEntries[j].Split('\\');
                    if (!episodeEntries[j].Contains('%'))
                    {
                        Log("Missing separator: " + namePath);
                        throw new ArgumentNullException();
                    }
                    string[] episodeNameNumber = namePath[namePath.Length - 1].Split('%');
                    int fileSuffixIndex = episodeNameNumber[1].LastIndexOf('.');
                    string episodeName = episodeNameNumber[1].Substring(0, fileSuffixIndex).Trim();
                    Episode episode = new Episode(0, episodeName, episodeEntries[j]);
                    season.Episodes[j] = episode;
                }
                show.Seasons[i] = season;
            }
            return show;
        }

        private void ProcessExtrasDirectory(List<Episode> extras, string targetDir)
        {
            string[] rootEntries = Directory.GetFiles(targetDir);
            foreach (string entry in rootEntries)
            {
                string[] namePath = entry.Split('\\');
                string[] episodeNameNumber = namePath[namePath.Length - 1].Split('%');
                int fileSuffixIndex;
                string episodeName;
                if (episodeNameNumber.Length == 1)
                {
                    fileSuffixIndex = episodeNameNumber[0].LastIndexOf('.');
                    episodeName = episodeNameNumber[0].Substring(0, fileSuffixIndex).Trim();
                }
                else
                {
                    fileSuffixIndex = episodeNameNumber[1].LastIndexOf('.');
                    episodeName = episodeNameNumber[1].Substring(0, fileSuffixIndex).Trim();
                }

                Episode ep = new Episode(-1, episodeName, entry);
                extras.Add(ep);
            }
            string[] subDirectories = Directory.GetDirectories(targetDir);
            foreach (string subDir in subDirectories)
            {
                ProcessExtrasDirectory(extras, subDir);
            }
        }

        private int SeasonComparer(string seasonB, string seasonA)
        {
            if (seasonB.Contains("Extras"))
            {
                return 1;
            }
            else if (seasonA.Contains("Extras"))
            {
                return -1;
            }
            string[] seasonValuePathA = seasonA.Split();
            string[] seasonValuePathB = seasonB.Split();
            int seasonValueA = Int32.Parse(seasonValuePathA[seasonValuePathA.Length - 1]);
            int seasonValueB = Int32.Parse(seasonValuePathB[seasonValuePathB.Length - 1]);
            if (seasonValueA == seasonValueB) return 0;
            if (seasonValueA < seasonValueB) return 1;
            return -1;
        }

        #endregion

        #region Background worker

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string mediaPath = ConfigurationManager.AppSettings["mediaPath"];
            string mediaPathB = ConfigurationManager.AppSettings["mediaPathB"];
            ProcessDirectory(mediaPath, mediaPathB);

            if (media == null)
            {
                Log("Media is null");
                throw new ArgumentNullException();
            }

            bool update = CheckForUpdates();
            if (update)
            {
                UpdateLoadingLabel(null);
                Task buildCache = BuildCacheAsync();
                buildCache.Wait();
            }
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingLabel.Dispose();
            this.Padding = new Padding(5, 20, 20, 20);
            layoutController = new LayoutController(media.Count);
            InitializeGui();
            layoutController.Initialize();
            worker.InitializeSerialPort(layoutController);
            loadingCircle1.Dispose();

            bool getLastEpisodeList = false;
            if (getLastEpisodeList)
            {
                for (int i = 0; i < media.TvShows.Length; i++)
                {
                    string lastEpisodeString = media.TvShows[i].LastEpisode == null ? "null" : media.TvShows[i].LastEpisode.Name + " (Season " + media.TvShows[i].CurrSeason + ")" + Environment.NewLine + media.TvShows[i].LastEpisode.Path;
                    Log(media.TvShows[i].Name + " Last episode: " + lastEpisodeString);
                }
            }

            idleMainFormTimer = new System.Threading.Timer(mt_ =>
            {
                if (PlayerForm.isPlaying)
                {
                    if (IsPaused())
                    {
                        if (idlePauseFormTimer == null)
                        {
                            idlePauseFormTimer = new System.Threading.Timer(pt_ =>
                            {
                                if (IsPaused())
                                {
                                    Log("Reached 2 hours of idle PAUSE time");
                                    Application.Exit();
                                }
                                else
                                {
                                    if (idlePauseFormTimer != null)
                                    {
                                        Log("dispose idlePauseFormTimer");
                                        idlePauseFormTimer.Dispose();
                                    }
                                }
                            }, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
                        }
                    }

                    return;
                }
                else
                {
                    if (idlePauseFormTimer != null)
                    {
                        Log("dispose idlePauseFormTimer 2");
                        idlePauseFormTimer.Dispose();
                    }
                }

                LASTINPUTINFO last = new LASTINPUTINFO();
                last.cbSize = (uint)LASTINPUTINFO.SizeOf;
                last.dwTime = 0u;
                if (GetLastInputInfo(ref last))
                {
                    TimeSpan idleTime = TimeSpan.FromMilliseconds(Environment.TickCount - last.dwTime);
                    if (idleTime > TimeSpan.FromMinutes(20))
                    {
                        Log("Reached 20 minutes of idle time");
                        Application.Exit();
                    }
                }
            }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        }

        private bool IsPaused()
        {
            PlayerForm currForm = null;
            FormCollection formCollection = Application.OpenForms;
            foreach (Form f_ in formCollection)
            {
                if (f_.Name.Equals("PlayerForm"))
                {
                    currForm = (PlayerForm)f_;
                }
            }
            if (currForm == null) throw new ArgumentNullException();

            if(currForm.mediaPlayer.IsPlaying)
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Mouse

        private void RestoreSystemCursor()
        {
            string[] keys = Properties.Resources.keys_backup.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string key in keys)
            {
                string[] keyValuePair = key.Split('=');
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors\", keyValuePair[0], keyValuePair[1]);
            }
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            SystemParametersInfo(0x2029, 0, 32, 0x01);
        }

        private void InitializeCustomCursor()
        {
            string cursorPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            string[] keys = Properties.Resources.keys_custom.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string key in keys)
            {
                string[] keyValuePair = key.Split('=');
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors\", keyValuePair[0], cursorPath + keyValuePair[1]);
            }
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            uint mouseSize = uint.Parse(ConfigurationManager.AppSettings["mouseSize"]);
            SystemParametersInfo(0x2029, 0, mouseSize, 0x01);
        }

        #endregion

        static public void Log(string message)
        {
            bool newLine = false;
            if (message == Environment.NewLine)
            {
                newLine = true;
            }
            if (debugLog)
            {
                using (StreamWriter sw = File.AppendText(debugLogPath))
                {
                    if (newLine)
                    {
                        sw.WriteLine();
                        return;
                    }
                    sw.WriteLine("{0} - {1}: {2}", DateTime.Now.ToString("dd-MM-yy HH:mm:ss.fff"), (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name, message);
                    Debug.WriteLine("{0} - {1}: {2}", DateTime.Now.ToString("dd-MM-yy HH:mm:ss.fff"), (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name, message);
                }
            }
        }
    }

    public class RoundButton : Button
    {
        // https://stackoverflow.com/questions/3708113/round-shaped-buttons
        protected override void OnPaint(PaintEventArgs e)
        {
            System.Drawing.Drawing2D.GraphicsPath grPath = new System.Drawing.Drawing2D.GraphicsPath();
            grPath.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new Region(grPath);
            base.OnPaint(e);
        }
    }

    public class NoHScrollTree : TreeView
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x8000; // TVS_NOHSCROLL
                return cp;
            }
        }
    }

    public static class StringExtension
    {
        private const string targetSingleQuoteSymbol = "'";
        private const string genericSingleQuoteSymbol = "â€™";
        private const string openSingleQuoteSymbol = "â€˜";
        private const string closeSingleQuoteSymbol = "â€™";
        private const string frenchAccentAigu = "Ã©";
        private const string frenchAccentGrave = "Ã";

        public static string fixBrokenQuotes(this string str)
        {
            return str.Replace(genericSingleQuoteSymbol, targetSingleQuoteSymbol).Replace(openSingleQuoteSymbol, targetSingleQuoteSymbol)
                .Replace(closeSingleQuoteSymbol, targetSingleQuoteSymbol).Replace(frenchAccentAigu, "e").Replace(frenchAccentGrave, "a");
        }
    }
}