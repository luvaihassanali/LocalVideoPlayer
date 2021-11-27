using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomControls;
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
        private const string tvSearch = apiUrl + "search/tv" + apiKey + "&query=";
        private const string movieSearch = apiUrl + "search/movie" + apiKey + "&query=";
        private string tvGet = apiUrl + "tv/{tv_id}" + apiKey;
        private string tvSeasonGet = apiUrl + "tv/{tv_id}/season/{season_number}" + apiKey;
        private string movieGet = apiUrl + "movie/{movie_id}" + apiKey;

        private string bufferString = "";
        private MediaModel media;
        private Label movieLabel;
        private Label tvLabel;
        private Form dimmerForm;
        private Form seasonDimmerForm;
        private bool seasonFormOpen = false;
        private bool isPlaying = false;
        private Panel mainFormMainPanel = null;
        private CustomScrollbar customScrollbar = null;

        public MainForm()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            InitializeComponent();

            this.DoubleBuffered = true;
            this.ControlBox = false;

            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.RunWorkerAsync();

            loadingCircle1.Active = true;

            dimmerForm = new Form();
            dimmerForm.ShowInTaskbar = false;
            dimmerForm.FormBorderStyle = FormBorderStyle.None;
            dimmerForm.BackColor = Color.Black;

            seasonDimmerForm = new Form();
            seasonDimmerForm.ShowInTaskbar = false;
            seasonDimmerForm.FormBorderStyle = FormBorderStyle.None;
            seasonDimmerForm.BackColor = Color.Black;
        }

        #region General form functions
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);

            dimmerForm.Close();
            seasonDimmerForm.Close();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //To-do: add some sort of text indicator for loading circle on initial launch
            loadingCircle1.Dispose();
            this.Padding = new System.Windows.Forms.Padding(5, 20, 20, 20);
            InitGui();
            //tvShowBox_Click(null, null); 
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //BackgroundWorker worker = sender as BackgroundWorker;

            string mediaPath = ConfigurationManager.AppSettings["mediaPath"];
            ProcessDirectory(mediaPath);

            if (media == null) throw new ArgumentNullException();

            bool update = CheckForUpdates();

            if (update)
            {
                //To-do: change to synchronous
                Task buildCache = BuildCacheAsync();
                buildCache.Wait();
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            Panel p = b.Parent as Panel;
            Form f;
            if (p != null)
            {
                f = (Form)p.Parent;
            }
            else
            {
                f = (Form)b.Parent;
            }

            f.Close();

            if (!f.Name.Equals("MainForm"))
            {
                Fader.FadeOut(dimmerForm, Fader.FadeSpeed.Normal);
            }
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

        private void LaunchVlc(string mediaName, string episodeName, string path)
        {
            TvShow currTvShow = null;
            Episode currEpisode = null;
            Movie currMovie = null;
            int currSeason = 0;

            //To-do: change to if episode name is null otherwise use media name to get movie + runningTime
            if (episodeName != null)
            {
                currTvShow = GetTvShow(mediaName);
                currEpisode = GetTvEpisode(mediaName, episodeName, out currSeason);

            }
            else
            {
                currMovie = GetMovie(mediaName);
            }

            //To-do: Fade in
            long savedTime = 0;
            if (currMovie == null)
            {
                if (currEpisode != null)
                {
                    savedTime = currEpisode.SavedTime;
                }
            }
            //To-do: all one line if-else replace with ? :
            int runningTime;
            if (currMovie == null)
            {
                runningTime = currTvShow.RunningTime;
            }
            else
            {
                runningTime = currMovie.RunningTime;
            }

            Form playerForm = new PlayerForm(path, savedTime, runningTime);
            playerForm.ShowDialog();

            if (currTvShow != null)
            {
                long endTime = long.Parse(playerForm.Text);
                if (endTime > 0) //600000 ms = 10 mins 
                {
                    if (currEpisode == null) throw new ArgumentNullException();
                    if (currSeason == 0) throw new ArgumentNullException();

                    currEpisode.SavedTime = endTime;
                    currTvShow.CurrSeason = currSeason;
                    currTvShow.LastEpisode = currEpisode;
                }
            }

            playerForm.Dispose();
            isPlaying = false;
        }

        private RoundButton CreateCloseButton()
        {
            RoundButton closeButton = new RoundButton();
            closeButton.BackgroundImage = Properties.Resources.close;
            closeButton.BackgroundImageLayout = ImageLayout.Zoom;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.Red;
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Location = new Point(728, 8);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(64, 64);
            //closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += closeButton_Click;
            return closeButton;
        }

        private void customScrollbar_Scroll(object sender, EventArgs e)
        {
            mainFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
            showHideClose(customScrollbar.Value);
        }

        private void mainFormMainPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            int newVal = -mainFormMainPanel.AutoScrollPosition.Y;
            showHideClose(newVal);
            customScrollbar.Value = newVal;
            customScrollbar.Invalidate();
            Application.DoEvents();
        }

        private void showHideClose(int value)
        {
            if (value == 0)
            {
                closeButton.Visible = true;
            }
            else
            {
                closeButton.Visible = false;
            }
        }

        #endregion 

        #region Tv

        private void tvShowBox_Click(object sender, EventArgs e)
        {
            Form tvForm = new Form();

            PictureBox pictureBox = null;
            if (sender != null)
            {
                pictureBox = sender as PictureBox;
            }
            else
            {
                foreach (Control c in this.Controls)
                {
                    if (c.Name.Contains("tv") && c is Panel)
                    {
                        Panel panel = c as Panel;
                        foreach (Control pc in panel.Controls)
                        {
                            if (pc.Name.Equals("Family Guy"))
                            {
                                pictureBox = pc as PictureBox;
                            }
                        }
                    }
                }
            }

            TvShow tvShow = GetTvShow(pictureBox.Name);

            tvForm.Name = tvShow.Name;
            tvForm.FormBorderStyle = FormBorderStyle.None;
            tvForm.Width = (int)(this.Width / 1.75);
            tvForm.Height = this.Height;
            tvForm.AutoScroll = true;
            tvForm.StartPosition = FormStartPosition.CenterScreen;
            tvForm.BackColor = SystemColors.Desktop;
            tvForm.ForeColor = SystemColors.Control;

            RoundButton closeButton = CreateCloseButton();
            tvForm.Controls.Add(closeButton);
            closeButton.Location = new Point(tvForm.Width - (int)(closeButton.Width * 1.45), (closeButton.Width / 8));

            Font f = new Font("Arial", 26, FontStyle.Bold);
            Font f3 = new Font("Arial", 16, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);
            Font f4 = new Font("Arial", 14, FontStyle.Bold);

            PictureBox tvShowBackdropBox = new PictureBox();
            tvShowBackdropBox.Height = (int)(tvForm.Height / 1.777777777777778);
            string imagePath = tvShow.Backdrop;
            tvShowBackdropBox.Image = Image.FromFile(imagePath);
            tvShowBackdropBox.Dock = DockStyle.Top;
            tvShowBackdropBox.SizeMode = PictureBoxSizeMode.StretchImage;
            tvShowBackdropBox.Name = tvShow.Name;
            //tvShowBackdropBox.Click += tvShowBackdropBox_Click;

            Panel mainPanel = new Panel();
            mainPanel.BackColor = SystemColors.Desktop;
            mainPanel.Dock = DockStyle.Top;
            mainPanel.AutoSize = true;
            mainPanel.Padding = new Padding(20);
            mainPanel.Name = "mainPanel";

            Label headerLabel = new Label() { Text = tvShow.Name + " (" + tvShow.Date.GetValueOrDefault().Year + ")" };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = f;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 0);
            headerLabel.Name = "headerLabel";

            Label overviewLabel = new Label() { Text = tvShow.Overview };
            overviewLabel.Dock = DockStyle.Top;
            overviewLabel.Font = f2;
            overviewLabel.AutoSize = true;
            overviewLabel.Padding = new Padding(20, 20, 20, 0);
            overviewLabel.MaximumSize = new Size(tvForm.Width - (overviewLabel.Width / 2), tvForm.Height);
            overviewLabel.Name = "overviewLabel";

            Label episodeHeaderLabel = new Label() { Text = "Episodes" };
            episodeHeaderLabel.Dock = DockStyle.Top;
            episodeHeaderLabel.Font = f3;
            episodeHeaderLabel.AutoSize = true;
            episodeHeaderLabel.Padding = new Padding(0, 0, 0, 20);
            episodeHeaderLabel.Name = "episodeHeaderLabel";

            Panel seasonButtonPanel = new Panel();
            seasonButtonPanel.AutoSize = true;
            seasonButtonPanel.Padding = new Padding(0, 0, 0, 20);
            seasonButtonPanel.Dock = DockStyle.Top;
            seasonButtonPanel.Name = "seasonButtonPanel";

            Button seasonButton = new Button();
            seasonButton.Dock = DockStyle.Top;
            seasonButton.AutoSize = true;
            seasonButton.Name = "season_" + tvShow.Name;
            seasonButton.Text = "Season " + tvShow.CurrSeason;
            seasonButton.Font = f3;
            seasonButton.Cursor = Cursors.Hand;
            seasonButton.FlatStyle = FlatStyle.Flat;
            seasonButton.FlatAppearance.MouseOverBackColor = SystemColors.ControlDarkDark;
            seasonButtonPanel.Controls.Add(seasonButton);
            seasonButton.Click += SeasonButton_Click;
            if (tvShow.Seasons[0].Poster == null || tvShow.Seasons[0].Equals(String.Empty)) throw new ArgumentNullException();

            /*ComboBox seasonComboBox = new ComboBox();
            seasonComboBox.AutoSize = true;
            seasonComboBox.Font = f3;
            seasonComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            seasonComboBox.Dock = DockStyle.Top;
            seasonComboBox.SelectedIndexChanged += new System.EventHandler(ComboBox1_SelectedIndexChanged);
            seasonComboBox.Sorted = true;
            for (int i = 0; i < tvShow.Seasons.Length; i++)
            {
                seasonComboBox.Items.Add("Season " + (i + 1));
            }
            seasonComboBox.SelectedIndex = 0;
            seasonComboBoxPanel.Controls.Add(seasonComboBox);*/

            List<Control> episodePanelList = CreateEpisodePanels(tvShow);

            episodePanelList.Reverse();

            mainPanel.Controls.AddRange(episodePanelList.ToArray());

            tvForm.Controls.Add(mainPanel);

            mainPanel.Controls.Add(seasonButtonPanel);

            mainPanel.Controls.Add(episodeHeaderLabel);

            tvForm.Controls.Add(overviewLabel);

            tvForm.Controls.Add(headerLabel);

            tvForm.Controls.Add(tvShowBackdropBox);

            tvForm.Deactivate += (s, ev) =>
            {
                if (seasonFormOpen || isPlaying) return;
                tvForm.Close();
                Fader.FadeOut(dimmerForm, Fader.FadeSpeed.Normal);
            };

            dimmerForm.Size = this.Size;
            Fader.FadeInCustom(dimmerForm, Fader.FadeSpeed.Normal, 0.8);
            dimmerForm.Location = this.Location;

            for (int i = 0; i < episodePanelList.Count; i++)
            {
                if (episodePanelList[i].Controls.Count == 4)
                {
                    episodePanelList[i].Controls[1].Location = new Point(episodePanelList[i].Controls[1].Location.X + 10, (episodePanelList[i].Height / 2) - (episodePanelList[i].Controls[1].Height / 2) + 5);
                    episodePanelList[i].Controls[0].Location = new Point(episodePanelList[i].Controls[1].Location.X, episodePanelList[i].Controls[1].Location.Y + episodePanelList[i].Controls[1].Height - 10);
                }
                else if (episodePanelList[i].Controls.Count == 3)
                {
                    episodePanelList[i].Controls[0].Location = new Point(episodePanelList[i].Controls[0].Location.X + 10, (episodePanelList[i].Height / 2) - (episodePanelList[i].Controls[0].Height / 2) + 5);
                }
                else
                {
                    throw new ArgumentNullException(); // something went wrong
                }
            }

            tvForm.Show();
        }


        private void UpdateTvForm(TvShow tvShow)
        {
            Form tvForm = Application.OpenForms[2];
            Panel mainPanel = null;

            Font f = new Font("Arial", 26, FontStyle.Bold);
            Font f3 = new Font("Arial", 16, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);
            Font f4 = new Font("Arial", 14, FontStyle.Bold);

            List<Control> toRemove = new List<Control>();
            foreach (Control c in tvForm.Controls)
            {
                Panel p = c as Panel;
                if (p != null && p.Name.Equals("mainPanel"))
                {
                    mainPanel = p;
                    foreach (Control c_ in mainPanel.Controls)
                    {
                        Panel ePanel = c_ as Panel;
                        if (ePanel != null && ePanel.Name.Contains("episodePanel"))
                        {
                            toRemove.Add(ePanel);
                        }
                    }
                }
            }
            foreach (Control c in toRemove)
            {
                mainPanel.Controls.Remove(c);
            }
            mainPanel.Refresh();

            List<Control> episodePanelList = CreateEpisodePanels(tvShow);

            foreach (Control ep in episodePanelList)
            {
                mainPanel.Controls.Add(ep);
                mainPanel.Controls.SetChildIndex(ep, 0);
            }

            for (int i = 0; i < episodePanelList.Count; i++)
            {
                if (episodePanelList[i].Controls.Count == 4)
                {
                    episodePanelList[i].Controls[1].Location = new Point(episodePanelList[i].Controls[1].Location.X + 10, (episodePanelList[i].Height / 2) - (episodePanelList[i].Controls[1].Height / 2) + 5);
                    episodePanelList[i].Controls[0].Location = new Point(episodePanelList[i].Controls[1].Location.X, episodePanelList[i].Controls[1].Location.Y + episodePanelList[i].Controls[1].Height - 10);
                }
                else if (episodePanelList[i].Controls.Count == 3)
                {
                    episodePanelList[i].Controls[0].Location = new Point(episodePanelList[i].Controls[0].Location.X + 10, (episodePanelList[i].Height / 2) - (episodePanelList[i].Controls[0].Height / 2) + 5);
                }
                else
                {
                    throw new ArgumentNullException(); // something went wrong
                }
            }

            mainPanel.Refresh();
        }

        private List<Control> CreateEpisodePanels(TvShow tvShow)
        {
            Font f2 = new Font("Arial", 12, FontStyle.Regular);
            Font f4 = new Font("Arial", 14, FontStyle.Bold);
            List<Control> episodePanelList = new List<Control>();
            Season currSeason = tvShow.Seasons[tvShow.CurrSeason - 1];
            for (int i = 0; i < currSeason.Episodes.Length; i++)
            {
                Episode currEpisode = currSeason.Episodes[i];

                Panel episodePanel = new Panel();
                episodePanel.BackColor = Color.FromArgb(20, 20, 20);
                episodePanel.Dock = DockStyle.Top;
                episodePanel.AutoSize = true;
                episodePanel.BorderStyle = BorderStyle.FixedSingle;
                episodePanel.Name = "episodePanel" + i + " " + currEpisode.Name;
                episodePanel.Padding = new Padding(10);
                episodePanelList.Add(episodePanel);

                PictureBox episodeBox = new PictureBox();
                //To-do: fix it
                episodeBox.Width = 300;
                //To-do: make const
                episodeBox.Height = (int)(episodeBox.Width / 1.777777777777778);
                string eImagePath = currEpisode.Backdrop;
                episodeBox.BackgroundImage = Image.FromFile(eImagePath);
                episodeBox.BackgroundImageLayout = ImageLayout.Stretch;
                episodeBox.BackColor = Color.Transparent;
                episodeBox.Cursor = Cursors.Hand;
                episodeBox.SizeMode = PictureBoxSizeMode.CenterImage;
                episodeBox.Click += tvShowEpisodeBox_Click;
                episodeBox.Name = currEpisode.Path;

                //To-do: set minimum time for bar to show up
                if (currEpisode.SavedTime != 0)
                {
                    ProgressBar progressBar = CreateProgressBar(currEpisode.SavedTime, tvShow.RunningTime);
                    episodePanel.Controls.Add(progressBar);
                }

                episodeBox.MouseEnter += (s, ev) =>
                {
                    episodeBox.Image = Properties.Resources.smallPlay;
                };

                episodeBox.MouseLeave += (s, ev) =>
                {
                    episodeBox.Image = null;
                };

                Label episodeNameLabel = new Label() { Text = currEpisode.Name };
                episodeNameLabel.Dock = DockStyle.Top;
                episodeNameLabel.Font = f4;
                episodeNameLabel.AutoSize = true;
                episodeNameLabel.Padding = new Padding(episodeBox.Width + 15, 10, 0, 15);
                //To-do: remove unecessary name tags
                episodeNameLabel.Name = "episodeNameLabel";

                Label episodeOverviewLabel = new Label() { Text = currEpisode.Overview };
                episodeOverviewLabel.Dock = DockStyle.Top;
                episodeOverviewLabel.Font = f2;
                episodeOverviewLabel.AutoSize = true;
                episodeOverviewLabel.Padding = new Padding(episodeBox.Width + 15, 10, 0, 10);
                episodeOverviewLabel.ForeColor = Color.LightGray;
                episodeOverviewLabel.MaximumSize = new Size((this.Width / 2) + (episodeBox.Width / 8), this.Height);
                episodeOverviewLabel.Name = "episodeOverviewLabel";

                //To-do: add running time > update index in for loop
                episodePanel.Controls.Add(episodeBox);
                episodePanel.Controls.Add(episodeOverviewLabel);
                episodePanel.Controls.Add(episodeNameLabel);
            }

            return episodePanelList;
        }

        private void SeasonButton_Click(object sender, EventArgs e)
        {
            seasonFormOpen = true;
            bool indexChange = false;

            Button b = sender as Button;
            string showName = b.Name.Replace("season_", "");
            int seasonNum = Int32.Parse(b.Text.Replace("Season ", ""));
            TvShow tvShow = GetTvShow(showName);
            //To-do: Scroll to current season / tv episode
            Form seasonForm = new Form();
            seasonForm.Width = (int)(this.Width / 2.75);
            seasonForm.Height = (int)(this.Height / 1.1);
            seasonForm.AutoScroll = true;
            seasonForm.Padding = new Padding(5, 5, 5, 5);
            seasonForm.StartPosition = FormStartPosition.CenterScreen;
            seasonForm.BackColor = SystemColors.Desktop;
            seasonForm.ForeColor = SystemColors.Control;
            seasonForm.FormBorderStyle = FormBorderStyle.None;
            seasonForm.FormClosing += (sender_, e_) =>
            {
                //To-do: Closing animation?
            };

            int numSeasons = tvShow.Seasons.Length;
            int currSeasonIndex = tvShow.CurrSeason - 1;

            Panel currentPanel = null;
            int count = 0;
            int panelCount = 0;

            for (int i = 0; i < numSeasons; i++)
            {
                if (count == 3) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Transparent;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    panelCount++;
                    seasonForm.Controls.Add(currentPanel);
                    seasonForm.Controls.SetChildIndex(currentPanel, 0);
                }

                Season currSeason = tvShow.Seasons[i];
                PictureBox seasonBox = new PictureBox();
                //To-do: fix?
                seasonBox.Width = (int)(seasonForm.Width / 3.1);
                seasonBox.Height = (int)(seasonBox.Width * 1.5);
                //To-do: No season images
                if (currSeason.Poster == null || currSeason.Poster.Equals(String.Empty)) throw new ArgumentNullException();
                string imagePath = currSeason.Poster;
                seasonBox.Image = Image.FromFile(imagePath);
                seasonBox.BackColor = Color.Transparent;
                seasonBox.Left = seasonBox.Width * currentPanel.Controls.Count;
                seasonBox.Cursor = Cursors.Hand;
                seasonBox.SizeMode = PictureBoxSizeMode.StretchImage;
                seasonBox.Padding = new Padding(5);
                seasonBox.Name = (i + 1).ToString();

                seasonBox.Click += (s, ev) =>
                {
                    seasonNum = Int32.Parse(seasonBox.Name);
                    indexChange = seasonNum == currSeasonIndex + 1 ? false : true;

                    PictureBox p = s as PictureBox;
                    foreach (Control c in seasonForm.Controls)
                    {
                        Panel p_ = c as Panel;
                        if (p_ != null)
                        {
                            foreach (Control c_ in p_.Controls)
                            {
                                PictureBox prevSeason = c_ as PictureBox;
                                if (prevSeason != null && Int32.Parse(prevSeason.Name) == seasonNum)
                                {
                                    prevSeason.BorderStyle = BorderStyle.None;
                                }
                            }
                        }
                    }
                    seasonBox.BorderStyle = BorderStyle.Fixed3D;

                    tvShow.CurrSeason = seasonNum;
                    b.Text = "Season " + seasonNum;
                    //To-do: Transitions...
                    seasonForm.Close();
                };

                if (i == currSeasonIndex)
                {
                    seasonBox.BorderStyle = BorderStyle.Fixed3D;
                }

                currentPanel.Controls.Add(seasonBox);
                count++;
            }

            Form tvForm = Application.OpenForms[1];
            seasonDimmerForm.Size = tvForm.Size;
            Fader.FadeInCustom(seasonDimmerForm, Fader.FadeSpeed.Normal, 0.8);
            seasonDimmerForm.Location = tvForm.Location;

            seasonForm.ShowDialog();
            seasonForm.Dispose();

            seasonFormOpen = false;
            Fader.FadeOut(seasonDimmerForm, Fader.FadeSpeed.Normal);

            if (indexChange)
            {
                UpdateTvForm(tvShow);
            }
        }

        private TvShow GetTvShow(string name)
        {
            for (int i = 0; i < media.TvShows.Length; i++)
            {
                if (media.TvShows[i].Name.Equals(name))
                {
                    return media.TvShows[i];
                }
            }
            return null;
        }

        private Episode GetTvEpisode(string showName, string episodeName, out int season)
        {
            for (int i = 0; i < media.TvShows.Length; i++)
            {
                if (media.TvShows[i].Name.Equals(showName))
                {
                    for (int j = 0; j < media.TvShows[i].Seasons.Length; j++)
                    {
                        Season currSeason = media.TvShows[i].Seasons[j];

                        for (int k = 0; k < currSeason.Episodes.Length; k++)
                        {
                            Episode currEpisode = currSeason.Episodes[k];
                            if (currEpisode.Name.Contains(episodeName))
                            {
                                season = j + 1;
                                return currEpisode;
                            }
                        }
                    }
                }
            }
            season = 0;
            return null;
        }

        private void tvShowEpisodeBox_Click(object sender, EventArgs e)
        {
            isPlaying = true;
            PictureBox p = sender as PictureBox;
            string path = p.Name;
            string[] pathSplit = path.Split('\\');
            string episodeName = pathSplit[pathSplit.Length - 1].Split('%')[1];
            episodeName = episodeName.Split('.')[0].Trim();
            string showName = pathSplit[pathSplit.Length - 3].Split('%')[0].Trim();
            LaunchVlc(showName, episodeName, path);

            TvShow tvShow = GetTvShow(showName);
            Episode lastEpisode = tvShow.LastEpisode;
            Panel episodePanel = (Panel)p.Parent;

            if (episodePanel.Controls.Count == 3)
            {
                ProgressBar progressBar = CreateProgressBar(lastEpisode.SavedTime, tvShow.RunningTime);
                progressBar.Location = new Point(p.Location.X, p.Location.Y + p.Height);
                episodePanel.Controls.Add(progressBar);
            }
            else if (episodePanel.Controls.Count == 4)
            {
                ProgressBar progressBar;
                foreach (Control c in episodePanel.Controls)
                {
                    if (c.Name.Equals("pBar"))
                    {
                        progressBar = c as ProgressBar;
                        TimeSpan duration = TimeSpan.FromMilliseconds(lastEpisode.SavedTime);
                        progressBar.Value = (int)duration.TotalMinutes;
                    }
                }
            }
            else
            {
                //something went wrong
                throw new ArgumentNullException();
            }


        }

        private ProgressBar CreateProgressBar(long savedTime, int runningTime)
        {
            //To-do: min time before showing and max time before filling completely
            ProgressBar progressBar = new ProgressBar();
            progressBar.Height = 10;
            progressBar.Width = 300;
            TimeSpan duration = TimeSpan.FromMilliseconds(savedTime);
            progressBar.Value = (int)duration.TotalMinutes;
            progressBar.Maximum = runningTime;
            progressBar.Name = "pBar";
            return progressBar;
        }

        #endregion

        #region Movies
        private void movieBox_Click(object sender, EventArgs e)
        {
            Form movieForm = new Form();
            PictureBox p = sender as PictureBox;
            Movie movie = GetMovie(p.Name);

            movieForm.Width = (int)(this.Width / 1.75);
            movieForm.Height = this.Height;
            movieForm.AutoScroll = true;
            movieForm.FormBorderStyle = FormBorderStyle.None;
            movieForm.StartPosition = FormStartPosition.CenterScreen;
            movieForm.BackColor = SystemColors.Desktop;
            movieForm.ForeColor = SystemColors.Control;

            RoundButton closeButton = CreateCloseButton();
            movieForm.Controls.Add(closeButton);
            closeButton.Location = new Point(movieForm.Width - (int)(closeButton.Width * 1.165), (closeButton.Width / 8));

            Font f = new Font("Arial", 24, FontStyle.Bold);
            Font f3 = new Font("Arial", 16, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);

            PictureBox movieBackdropBox = new PictureBox();
            movieBackdropBox.Height = (int)(movieForm.Height / 1.777777777777778);
            string imagePath = movie.Backdrop;
            movieBackdropBox.BackgroundImage = Image.FromFile(imagePath);
            movieBackdropBox.BackgroundImageLayout = ImageLayout.Stretch;
            movieBackdropBox.BackColor = Color.Transparent;
            movieBackdropBox.Dock = DockStyle.Top;
            movieBackdropBox.Cursor = Cursors.Hand;
            movieBackdropBox.SizeMode = PictureBoxSizeMode.CenterImage;
            movieBackdropBox.Name = movie.Path;
            movieBackdropBox.Click += movieBackdropBox_Click;
            movieBackdropBox.MouseEnter += (s, ev) =>
            {
                movieBackdropBox.Image = Properties.Resources.play;
            };
            movieBackdropBox.MouseLeave += (s, ev) =>
            {
                movieBackdropBox.Image = null;
            };
            Label headerLabel = new Label() { Text = movie.Name + " (" + movie.Date.GetValueOrDefault().Year + ")" };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = f;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 0);

            Label overviewLabel = new Label() { Text = movie.Overview };
            overviewLabel.Dock = DockStyle.Top;
            overviewLabel.Font = f2;
            overviewLabel.AutoSize = true;
            overviewLabel.Padding = new Padding(20, 20, 20, 0);
            overviewLabel.MaximumSize = new Size(movieForm.Width - (overviewLabel.Width / 2), movieForm.Height);

            movieForm.Controls.Add(overviewLabel);

            movieForm.Controls.Add(headerLabel);

            movieForm.Controls.Add(movieBackdropBox);

            movieForm.Deactivate += (s, ev) =>
            {
                if (isPlaying) return;
                movieForm.Close();
                Fader.FadeOut(dimmerForm, Fader.FadeSpeed.Normal);

            };

            dimmerForm.Size = this.Size;
            Fader.FadeInCustom(dimmerForm, Fader.FadeSpeed.Normal, 0.8);
            dimmerForm.Location = this.Location;

            movieForm.Show();
        }
        private void movieBackdropBox_Click(object sender, EventArgs e)
        {
            PictureBox p = sender as PictureBox;
            string path = p.Name;
            LaunchVlc(null, null, path);
        }
        private Movie GetMovie(object name)
        {
            for (int i = 0; i < media.Movies.Length; i++)
            {
                if (media.Movies[i].Name.Equals(name))
                {
                    return media.Movies[i];
                }
            }
            return null;
        }

        #endregion 

        #region Startup

        private void InitGui()
        {
            mainFormMainPanel = new Panel();
            mainFormMainPanel.Size = this.Size;
            mainFormMainPanel.AutoScroll = true;
            mainFormMainPanel.Name = "mainFormMainPanel";
            mainFormMainPanel.MouseWheel += mainFormMainPanel_MouseWheel;
            mainFormMainPanel.Width += 20;
            this.Controls.Add(mainFormMainPanel);

            closeButton.Visible = true;
            closeButton.Location = new Point(mainFormMainPanel.Width - (int)(closeButton.Width * 1.75), (closeButton.Width / 8));

            //To-do: no media exists
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
                    currentPanel.BackColor = Color.Transparent;
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
                movieBox.Image = Image.FromFile(imagePath);
                movieBox.BackColor = Color.Transparent;
                movieBox.Left = movieBox.Width * currentPanel.Controls.Count;
                movieBox.Cursor = Cursors.Hand;
                movieBox.SizeMode = PictureBoxSizeMode.StretchImage;
                movieBox.Padding = new Padding(20);
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
                    currentPanel.BackColor = Color.Transparent;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "tv" + panelCount;
                    panelCount++;
                    mainFormMainPanel.Controls.Add(currentPanel);
                    mainFormMainPanel.Controls.SetChildIndex(currentPanel, 4);
                }

                PictureBox tvShowBox = new PictureBox();
                tvShowBox.Width = widthValue;
                tvShowBox.Height = heightValue;
                string imagePath = media.TvShows[i].Poster;
                tvShowBox.Image = Image.FromFile(imagePath);
                tvShowBox.BackColor = Color.Transparent;
                tvShowBox.Left = tvShowBox.Width * currentPanel.Controls.Count;
                tvShowBox.Cursor = Cursors.Hand;
                tvShowBox.SizeMode = PictureBoxSizeMode.StretchImage;
                tvShowBox.Padding = new Padding(20);
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
            mainFormMainPanel.Controls.Add(tvLabel);

            customScrollbar = new CustomScrollbar();
            customScrollbar.ChannelColor = Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(166)))), ((int)(((byte)(3)))));
            customScrollbar.Location = new Point(mainFormMainPanel.Width - 37, 0);
            customScrollbar.Size = new Size(15, this.Height - 2);
            customScrollbar.DownArrowImage = Properties.Resources.downarrow;
            customScrollbar.ThumbBottomImage = Properties.Resources.ThumbBottom;
            customScrollbar.ThumbBottomSpanImage = Properties.Resources.ThumbSpanBottom;
            customScrollbar.ThumbMiddleImage = Properties.Resources.ThumbMiddle;
            customScrollbar.ThumbTopImage = Properties.Resources.ThumbTop;
            customScrollbar.ThumbTopSpanImage = Properties.Resources.ThumbSpanTop;
            customScrollbar.UpArrowImage = Properties.Resources.uparrow;
            customScrollbar.Minimum = 0;
            customScrollbar.Maximum = mainFormMainPanel.DisplayRectangle.Height;
            customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + (int)(mainFormMainPanel.Height / 1);
            customScrollbar.SmallChange = 1;
            customScrollbar.Value = Math.Abs(mainFormMainPanel.AutoScrollPosition.Y);
            customScrollbar.Scroll += customScrollbar_Scroll;
            this.Controls.Add(customScrollbar);
            customScrollbar.BringToFront();
            //To-do: implement close button logic for other forms? 
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
                media.Ingest(prevMedia);
            }

            return result;
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
                    //if id is not 0 expected to be init
                    if (media.Movies[i].Id != 0) continue;

                    Movie movie = media.Movies[i];
                    string movieResourceString = client.DownloadString(movieSearch + movie.Name);

                    JObject movieObject = JObject.Parse(movieResourceString);
                    int totalResults = (int)movieObject["total_results"];

                    if (totalResults == 0)
                    {
                        CustomMessageDialog("Error", "No movie found for: " + movie.Name);
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
                        movie.Id = ShowOptionsDialog(movie.Name, info, dates);
                    }
                    else
                    {
                        movie.Id = (int)movieObject["results"][0]["id"];
                    }
                    //To-do: 404 not found
                    string movieString = client.DownloadString(movieGet.Replace("{movie_id}", movie.Id.ToString()));
                    movieObject = JObject.Parse(movieString);

                    if (String.Compare(movie.Name.Replace(":", ""), ((string)movieObject["title"]).Replace(":", ""), System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
                    {
                        movie.Backdrop = (string)movieObject["backdrop_path"];
                        movie.Poster = (string)movieObject["poster_path"];
                        movie.Overview = (string)movieObject["overview"];
                        //To-do: Add to gui
                        movie.RunningTime = (int)movieObject["runtime"];

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
                        CustomMessageDialog("Warning", message);

                        string oldPath = movie.Path;
                        string[] fileNamePath = oldPath.Split('\\');
                        string fileName = fileNamePath[fileNamePath.Length - 1];
                        string extension = fileName.Split('.')[1];
                        string newFileName = ((string)movieObject["title"]).Replace(":", "");
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

                    //if id is not 0 expected to be init
                    if (tvShow.Id == 0)
                    {
                        string tvResourceString = client.DownloadString(tvSearch + tvShow.Name);

                        JObject tvObject = JObject.Parse(tvResourceString);
                        int totalResults = (int)tvObject["total_results"];

                        if (totalResults == 0)
                        {
                            CustomMessageDialog("Error", "No tv show for: " + tvShow.Name);
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
                            tvShow.Id = ShowOptionsDialog(tvShow.Name, info, dates);
                        }
                        else
                        {
                            tvShow.Id = (int)tvObject["results"][0]["id"];
                        }

                        string tvString = client.DownloadString(tvGet.Replace("{tv_id}", tvShow.Id.ToString()));
                        tvObject = JObject.Parse(tvString);
                        tvShow.Overview = (string)tvObject["overview"];
                        tvShow.Poster = (string)tvObject["poster_path"];
                        tvShow.Backdrop = (string)tvObject["backdrop_path"];
                        tvShow.RunningTime = (int)tvObject["episode_run_time"][0];

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

                    int seasonIndex = 0;
                    for (int j = 0; j < tvShow.Seasons.Length; j++)
                    {
                        Season season = tvShow.Seasons[j];
                        string seasonApiCall = tvSeasonGet.Replace("{tv_id}", tvShow.Id.ToString()).Replace("{season_number}", seasonIndex.ToString());
                        string seasonString = client.DownloadString(seasonApiCall);
                        JObject seasonObject = JObject.Parse(seasonString);

                        if (!((string)seasonObject["name"]).Contains("Season"))
                        {
                            seasonIndex++;
                            seasonString = client.DownloadString(tvSeasonGet.Replace("{tv_id}", tvShow.Id.ToString()).Replace("{season_number}", seasonIndex.ToString()));
                            seasonObject = JObject.Parse(seasonString);
                        }

                        //To-do: what if poster is null
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
                            if (episodes[k].Id != 0) continue;

                            JObject jEpisode = (JObject)jEpisodes[k];
                            Episode episode = episodes[k];

                            if (String.Compare(episode.Name, (string)jEpisode["name"],
                                System.Globalization.CultureInfo.CurrentCulture, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreSymbols) == 0)
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
                                CustomMessageDialog("Warning", message);

                                string oldPath = episode.Path;
                                string newPath = oldPath.Replace(episode.Name, (string)jEpisode["name"]);
                                string invalid = new string(Path.GetInvalidPathChars()) + '?';
                                foreach (char c in invalid)
                                {
                                    newPath = newPath.Replace(c.ToString(), "");
                                }
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
                        seasonIndex++; //To-do: can be removed..?
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

        private void ProcessDirectory(string targetDir)
        {
            string moviesDir = null;
            string tvDir = null;
            string[] subdirectoryEntries = Directory.GetDirectories(targetDir);
            foreach (string subDir in subdirectoryEntries)
            {
                string[] subDirPath = subDir.Split('\\');
                string targetSubDir = subDirPath[subDirPath.Length - 1].ToLower();
                if (targetSubDir.ToLower().Equals("movies"))
                {
                    moviesDir = subDir;
                }
                if (targetSubDir.Equals("tv"))
                {
                    tvDir = subDir;
                }
            }
            if (moviesDir == null || tvDir == null) throw new ArgumentNullException();

            int moviesCount = Directory.GetDirectories(moviesDir).Length;
            int tvCount = Directory.GetDirectories(tvDir).Length;
            media = new MediaModel(moviesCount, tvCount);

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
            Array.Sort(seasonEntries, seasonComparer);
            show.Seasons = new Season[seasonEntries.Length];
            for (int i = 0; i < seasonEntries.Length; i++)
            {
                if (!seasonEntries[i].Contains("Season")) continue;
                Season season = new Season(i + 1);
                string[] episodeEntries = Directory.GetFiles(seasonEntries[i]);
                Array.Sort(episodeEntries);
                season.Episodes = new Episode[episodeEntries.Length];
                for (int j = 0; j < episodeEntries.Length; j++)
                {
                    string[] namePath = episodeEntries[j].Split('\\');
                    string[] episodeNameNumber = namePath[namePath.Length - 1].Split('%');
                    int fileSuffixIndex = episodeNameNumber[1].LastIndexOf('.');
                    //string episodeName = episodeNameNumber[1].Split('.')[0].Trim();
                    string episodeName = episodeNameNumber[1].Substring(0, fileSuffixIndex).Trim();
                    Episode episode = new Episode(0, episodeName, episodeEntries[j]);
                    season.Episodes[j] = episode;
                }
                show.Seasons[i] = season;
            }
            return show;
        }

        private int seasonComparer(string seasonB, string seasonA)
        {
            string[] seasonValuePathA = seasonA.Split();
            string[] seasonValuePathB = seasonB.Split();
            int seasonValueA = Int32.Parse(seasonValuePathA[seasonValuePathA.Length - 1]);
            int seasonValueB = Int32.Parse(seasonValuePathB[seasonValuePathB.Length - 1]);
            if (seasonValueA == seasonValueB) return 0;
            if (seasonValueA < seasonValueB) return 1;
            return -1;
        }

        #endregion

        #region Custom forms

        //To-do: create .cs for forms?
        private void CustomMessageDialog(string header, string message)
        {
            Form customMessageForm = new Form();
            customMessageForm.Width = this.Width / 2;
            customMessageForm.Height = this.Height / 6;
            customMessageForm.MaximumSize = new Size(this.Width, this.Height);

            customMessageForm.AutoScroll = true;
            customMessageForm.AutoSize = true;
            customMessageForm.Text = header;
            customMessageForm.StartPosition = FormStartPosition.CenterScreen;
            customMessageForm.BackColor = SystemColors.Desktop;
            customMessageForm.ForeColor = SystemColors.Control;
            customMessageForm.FormBorderStyle = FormBorderStyle.FixedDialog;

            Font f = new Font("Arial", 14, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);

            Label textLabel = new Label() { Text = message };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = f2;
            textLabel.AutoSize = true;
            textLabel.Padding = new Padding(20);
            Size maxSize = new Size(this.Width / 2, this.Height);
            textLabel.MaximumSize = maxSize;
            customMessageForm.Controls.Add(textLabel);

            Label headerLabel = new Label() { Text = header };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = f;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20);
            customMessageForm.Controls.Add(headerLabel);

            Button confirmation = new Button() { Text = "OK" };
            confirmation.AutoSize = true;
            confirmation.Font = f2;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = Cursors.Hand;
            confirmation.Click += (sender, e) => { customMessageForm.Close(); };
            customMessageForm.Controls.Add(confirmation);

            customMessageForm.ShowDialog();
            customMessageForm.Dispose();
        }

        private int ShowOptionsDialog(string item, string[][] info, DateTime?[] dates)
        {
            Form optionsForm = new Form();
            optionsForm.Width = (int)(this.Width / 1.5);
            optionsForm.Height = this.Height / 6;
            optionsForm.MaximumSize = new Size(this.Width, this.Height - 100);
            optionsForm.AutoSize = true;
            optionsForm.StartPosition = FormStartPosition.CenterScreen;
            optionsForm.BackColor = SystemColors.Desktop;
            optionsForm.ForeColor = SystemColors.Control;
            optionsForm.FormBorderStyle = FormBorderStyle.None;

            Panel optionsFormMainPanel = new Panel();
            optionsFormMainPanel.Size = optionsForm.Size;
            optionsFormMainPanel.MaximumSize = optionsForm.MaximumSize;
            optionsFormMainPanel.AutoSize = true;
            optionsFormMainPanel.AutoScroll = true;
            optionsFormMainPanel.Name = "optionsFormMainPanel";
            optionsForm.Controls.Add(optionsFormMainPanel);

            optionsForm.Shown += (s, e) =>
            {
                optionsFormMainPanel.AutoScrollPosition = new Point(0, 0);
            };

            Font f = new Font("Arial", 14, FontStyle.Bold);
            Font f2 = new Font("Arial", 12, FontStyle.Regular);

            Label textLabel = new Label() { Text = "Choose a selection for: " + item };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = f;
            textLabel.AutoSize = true;
            textLabel.Padding = new Padding(20);
            Size maxSize = new Size(this.Width / 2, this.Height);
            textLabel.MaximumSize = maxSize;

            List<Control> controls = new List<Control>();

            Button confirmation = new Button() { Text = "OK" };
            confirmation.AutoSize = true;
            confirmation.Font = f2;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = Cursors.Hand;
            confirmation.Click += (sender, e) =>
            {
                bool selection = false;
                foreach (Control c in controls)
                {
                    RadioButton btn = c as RadioButton;
                    if (btn != null)
                    {
                        if (btn.Checked)
                        {
                            selection = true;
                        }
                    }
                }

                if (selection)
                {
                    optionsForm.Close();
                }
            };

            for (int i = 0; i < info[0].Length; i++)
            {
                RadioButton r1 = new RadioButton { Text = info[0][i] + " (" + dates[i].GetValueOrDefault().Year + ")" };
                r1.Dock = DockStyle.Top;
                r1.Font = f2;
                r1.AutoSize = true;
                r1.Cursor = Cursors.Hand;
                r1.Padding = new Padding(20, 20, 20, 0);
                r1.Name = info[1][i];
                r1.Click += (sender, e) =>
                {
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirmation.Location.X, confirmation.Location.Y);
                };
                controls.Add(r1);

                Label l1 = new Label() { Text = info[2][i].Equals(String.Empty) ? "No description." : info[2][i] };
                l1.Dock = DockStyle.Top;
                l1.Font = f2;
                l1.AutoSize = true;
                l1.Padding = new Padding(20);
                Size s = new Size(optionsForm.Width - (l1.Width / 2), optionsForm.Height);
                l1.MaximumSize = s;
                l1.Cursor = Cursors.Hand;
                l1.Click += (sender, e) =>
                {
                    r1.Checked = true;
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirmation.Location.X, confirmation.Location.Y);
                };
                controls.Add(l1);
            }

            optionsFormMainPanel.Controls.Add(textLabel);
            optionsFormMainPanel.Controls.Add(confirmation);

            controls.Reverse();
            foreach (Control c in controls)
            {
                optionsFormMainPanel.Controls.Add(c);
            }
            optionsFormMainPanel.Controls.Add(textLabel);

            //To-do: make function
            CustomScrollbar customScrollbar = new CustomScrollbar();
            customScrollbar.ChannelColor = Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(166)))), ((int)(((byte)(3)))));
            customScrollbar.Location = new Point(optionsFormMainPanel.Width - 36, 0);
            customScrollbar.Size = new Size(15, optionsFormMainPanel.Height);
            customScrollbar.DownArrowImage = Properties.Resources.downarrow;
            customScrollbar.ThumbBottomImage = Properties.Resources.ThumbBottom;
            customScrollbar.ThumbBottomSpanImage = Properties.Resources.ThumbSpanBottom;
            customScrollbar.ThumbMiddleImage = Properties.Resources.ThumbMiddle;
            customScrollbar.ThumbTopImage = Properties.Resources.ThumbTop;
            customScrollbar.ThumbTopSpanImage = Properties.Resources.ThumbSpanTop;
            customScrollbar.UpArrowImage = Properties.Resources.uparrow;
            customScrollbar.Minimum = 0;
            customScrollbar.Maximum = optionsFormMainPanel.DisplayRectangle.Height;
            customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + (int)(optionsFormMainPanel.Height / 1);
            customScrollbar.SmallChange = 1;
            customScrollbar.Value = Math.Abs(optionsFormMainPanel.AutoScrollPosition.Y);
            customScrollbar.Scroll += (s, e) =>
            {
                optionsFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
            };
            optionsFormMainPanel.MouseWheel += (s, e) =>
            {
                int newVal = -optionsFormMainPanel.AutoScrollPosition.Y;
                customScrollbar.Value = newVal;
                customScrollbar.Invalidate();
                Application.DoEvents();
            };
            optionsForm.Controls.Add(customScrollbar);
            customScrollbar.BringToFront();
            optionsFormMainPanel.Width -= 20;

            optionsForm.ShowDialog();
            optionsForm.Dispose();

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

        #endregion
    }
}