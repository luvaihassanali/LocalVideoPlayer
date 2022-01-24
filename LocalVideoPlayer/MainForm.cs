using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

        private const string jsonFile = "Media.json";
        private const string apiKey = "?api_key=c69c4effc7beb9c473d22b8f85d59e4c";
        private const string apiUrl = "https://api.themoviedb.org/3/";
        private const string apiImageUrl = "http://image.tmdb.org/t/p/original";
        private const string tvSearch = apiUrl + "search/tv" + apiKey + "&query=";
        private const string movieSearch = apiUrl + "search/movie" + apiKey + "&query=";

        private const int SPI_SETCURSORS = 0x0057;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private string tvGet = apiUrl + "tv/{tv_id}" + apiKey;
        private string tvSeasonGet = apiUrl + "tv/{tv_id}/season/{season_number}" + apiKey;
        private string movieGet = apiUrl + "movie/{movie_id}" + apiKey;
        private string bufferString = "";

        private bool seasonFormOpen = false;
        private bool resetFormOpen = false;
        private bool isPlaying = false;
        private bool mouseMoverClientKill = false;

        private Form dimmerForm;
        private Form seasonDimmerForm;
        private Label movieLabel;
        private Label tvLabel;
        private MediaModel media;
        private Panel mainFormMainPanel = null;
        private CustomScrollbar customScrollbar = null;
        private Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);
        private MouseWorker worker = null;

        public MainForm()
        {
            Process applicaitionProcess = Process.GetCurrentProcess();
            applicaitionProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

            Process[] p = Process.GetProcessesByName("MouseMoverClient");
            if (p.Length != 0)
            {
                Process.GetProcessesByName("MouseMoverClient")[0].Kill();
                mouseMoverClientKill = true;
            }
            worker = new MouseWorker(this);
            worker.Start();

            UpdateSystemCursor();
            InitializeComponent();

            backgroundWorker1.DoWork += new DoWorkEventHandler(BackgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.RunWorkerAsync();

            loadingCircle1.Active = true;
            loadingLabel.Text = "";

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

        private void MainForm_Load(object sender, EventArgs e)
        {
            loadingCircle1.Location = new Point(this.Width / 2 - loadingCircle1.Width / 2, this.Height / 2 - loadingCircle1.Height / 2);
            loadingLabel.Location = new Point(0, this.Height / 2 - loadingLabel.Height / 2 + 2);
            loadingLabel.Size = new Size(this.Width, loadingLabel.Height); ;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (media != null)
            {
                string jsonString = JsonConvert.SerializeObject(media);
                File.WriteAllText(jsonFile, jsonString);
            }

            dimmerForm.Close();
            seasonDimmerForm.Close();
            RestoreSystemCursor();

            worker.StopImmediately();
            worker = null;

            if (mouseMoverClientKill)
            {
                string mouseMoverPath = ConfigurationManager.AppSettings["mouseMoverPath"];
                Process.Start(mouseMoverPath);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
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
            customScrollbar.Invalidate();
            Application.DoEvents();
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

        #region Tv

        private void TvShowEpisodeBox_Click(object sender, EventArgs e)
        {
            isPlaying = true;
            PictureBox p = sender as PictureBox;
            Form tvForm = (Form)p.Parent.Parent.Parent.Parent;
            string path = p.Name;
            string[] pathSplit = path.Split('\\');
            string episodeName;

            if (pathSplit[pathSplit.Length - 1].Contains('%'))
            {
                episodeName = pathSplit[pathSplit.Length - 1].Split('%')[1];
                episodeName = episodeName.Split('.')[0].Trim();
            }
            else
            {
                episodeName = pathSplit[pathSplit.Length - 1].Split('.')[0].Trim();
            }

            string showName = pathSplit[3].Split('%')[0].Trim();
            LaunchVlc(showName, episodeName, path, tvForm);
        }

        private void TvShowBox_Click(object sender, EventArgs e)
        {
            Form tvForm = new TvForm();

            PictureBox pictureBox = null;
            if (sender != null)
            {
                pictureBox = sender as PictureBox;
            }
            else
            {
                //To-do: Can remove
                foreach (Control c_ in this.Controls)
                {
                    Panel p = c_ as Panel;
                    if (p != null && p.Name.Contains("mainFormMainPanel"))
                    {
                        foreach (Control c in p.Controls)
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
                }
            }

            TvShow tvShow = GetTvShow(pictureBox.Name);

            tvForm.Name = tvShow.Name;
            tvForm.Width = (int)(this.Width / 1.75);
            tvForm.Height = this.Height;

            Panel tvFormMainPanel = new Panel();
            tvFormMainPanel.Size = tvForm.Size;
            tvFormMainPanel.AutoScroll = true;
            tvFormMainPanel.Name = "tvFormMainPanel";
            tvForm.Controls.Add(tvFormMainPanel);

            List<Control> episodePanelList = null;
            Button seasonButton = null;

            RoundButton closeButton = null;
            foreach (Control c in tvForm.Controls)
            {
                if (c.Name.Equals("closeButton"))
                    closeButton = (RoundButton)c;
            }
            closeButton.Click += CloseButton_Click;
            closeButton.Location = new Point(tvForm.Width - (int)(closeButton.Width * 1.65), (closeButton.Width / 8));

            Font mainHeaderFont = new Font("Arial", 26, FontStyle.Bold);
            Font episodeHeaderFont = new Font("Arial", 16, FontStyle.Bold);
            Font overviewFont = new Font("Arial", 12, FontStyle.Regular);

            PictureBox tvShowBackdropBox = new PictureBox();
            tvShowBackdropBox.Height = (int)(tvForm.Height / 1.777777777777778);
            string imagePath = tvShow.Backdrop;
            tvShowBackdropBox.Image = Image.FromFile(imagePath);
            tvShowBackdropBox.Dock = DockStyle.Top;
            tvShowBackdropBox.SizeMode = PictureBoxSizeMode.StretchImage;
            tvShowBackdropBox.Name = "tvShowBackdropBox";

            Button resumeButton = null;
            Button resetButton = null;
            if (tvShow.LastEpisode != null)
            {
                int currSeason = 0;
                Episode iAmLazyFuckEpisode = GetTvEpisode(tvShow.Name, tvShow.LastEpisode.Name, out currSeason);
                tvShow.CurrSeason = currSeason;

                foreach (Control c in tvForm.Controls)
                {
                    if (c.Name.Equals("resumeButton"))
                        resumeButton = (Button)c;

                    if (c.Name.Equals("resetButton"))
                        resetButton = (Button)c;
                }

                resetButton.Visible = true;
                resetButton.BringToFront();
                resetButton.Padding = new Padding(1, 0, 0, 0);
                resetButton.AutoSize = true;
                resetButton.Location = new Point(tvForm.Width - resetButton.Width - 35, tvShowBackdropBox.Location.Y + tvShowBackdropBox.Height - resetButton.Height - 5);
                resetButton.Cursor = blueHandCursor;
                resetButton.Click += (s, e_) =>
                {
                    resetFormOpen = true;
                    int[] seasons = CustomDialog.ShowResetSeasons(tvShow.Name, tvShow.Seasons.Length, this.Width, this.Height);
                    if (seasons.Length != 0)
                        ResetSeasons(tvShow, seasons);
                    resetFormOpen = false;
                };

                resumeButton.Visible = true;
                resumeButton.BringToFront();
                resumeButton.Padding = new Padding(3, 0, 0, 0);
                resumeButton.Font = mainHeaderFont;
                resumeButton.AutoSize = true;
                resumeButton.Location = new Point(tvShowBackdropBox.Location.X + 10, tvShowBackdropBox.Location.Y + 10);
                resumeButton.Cursor = blueHandCursor;
                resumeButton.Click += (s, e_) =>
                {
                    isPlaying = true;
                    Episode lastEpisode = tvShow.LastEpisode;
                    LaunchVlc(tvShow.Name, lastEpisode.Name, lastEpisode.Path, tvForm);
                    isPlaying = false;
                };
            }

            Panel mainPanel = new Panel();
            mainPanel.BackColor = SystemColors.Desktop;
            mainPanel.Dock = DockStyle.Top;
            mainPanel.AutoSize = true;
            mainPanel.Padding = new Padding(20);
            mainPanel.Name = "mainPanel";

            Label headerLabel = new Label() { Text = tvShow.Name + " (" + tvShow.Date.GetValueOrDefault().Year + ")" };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = mainHeaderFont;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 0);
            headerLabel.Name = "headerLabel";

            Label overviewLabel = new Label() { Text = tvShow.Overview };
            overviewLabel.Dock = DockStyle.Top;
            overviewLabel.Font = overviewFont;
            overviewLabel.AutoSize = true;
            overviewLabel.Padding = new Padding(20, 20, 20, 0);
            overviewLabel.MaximumSize = new Size(tvForm.Width - (overviewLabel.Width / 2), tvForm.Height);
            overviewLabel.Name = "overviewLabel";

            Label episodeHeaderLabel = new Label() { Text = "Episodes" };
            episodeHeaderLabel.Dock = DockStyle.Top;
            episodeHeaderLabel.Font = episodeHeaderFont;
            episodeHeaderLabel.AutoSize = true;
            episodeHeaderLabel.Padding = new Padding(0, 0, 0, 80);
            episodeHeaderLabel.Name = "episodeHeaderLabel";

            episodePanelList = CreateEpisodePanels(tvShow);
            episodePanelList.Reverse();
            mainPanel.Controls.AddRange(episodePanelList.ToArray());

            tvFormMainPanel.Controls.Add(mainPanel);
            mainPanel.Controls.Add(episodeHeaderLabel);

            tvFormMainPanel.Controls.Add(overviewLabel);
            tvFormMainPanel.Controls.Add(headerLabel);
            tvFormMainPanel.Controls.Add(tvShowBackdropBox);

            tvForm.Deactivate += (s, ev) =>
            {
                if (seasonFormOpen || isPlaying || resetFormOpen) return;
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
            }

            CustomScrollbar customScrollbar = CustomDialog.CreateScrollBar(tvFormMainPanel);
            customScrollbar.Scroll += (s, e_) =>
            {
                seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));
                tvFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
                if (customScrollbar.Value == 0)
                {
                    closeButton.Visible = true;
                    if (resumeButton != null)
                    {
                        resumeButton.Visible = true;
                        resetButton.Visible = true;
                    }
                }
                else
                {
                    closeButton.Visible = false;
                    if (resumeButton != null)
                    {
                        resumeButton.Visible = false;
                        resetButton.Visible = false;
                    }
                }
            };

            tvFormMainPanel.MouseWheel += (s, e_) =>
            {
                seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));
                int newVal = -tvFormMainPanel.AutoScrollPosition.Y;
                if (newVal == 0)
                {
                    closeButton.Visible = true;
                    if (resumeButton != null)
                    {
                        resumeButton.Visible = true;
                        resetButton.Visible = true;
                    }
                }
                else
                {
                    closeButton.Visible = false;
                    if (resumeButton != null)
                    {
                        resumeButton.Visible = false;
                        resetButton.Visible = false;
                    }
                }
                customScrollbar.Value = newVal;
                customScrollbar.Invalidate();
                Application.DoEvents();
            };

            tvForm.Controls.Add(customScrollbar);
            customScrollbar.BringToFront();

            foreach (Control c in tvForm.Controls)
            {
                if (c.Name.Equals("seasonButton"))
                    seasonButton = (Button)c;
            }

            if (tvShow.CurrSeason == -1)
                seasonButton.Text = "Extras";

            seasonButton.Name = "seasonButton_" + tvShow.Name;
            seasonButton.Text = "Season " + tvShow.CurrSeason;
            seasonButton.Font = episodeHeaderFont;
            seasonButton.Visible = true;
            seasonButton.Click += SeasonButton_Click;
            seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));
            seasonButton.Size = new Size(episodePanelList[0].Width - 18, seasonButton.Height);
            seasonButton.Cursor = blueHandCursor;

            seasonButton.MouseWheel += (s, e_) =>
            {
                if (e_.Delta < 0) //if scrolling down
                {
                    this.Cursor = new Cursor(Cursor.Current.Handle);
                    Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y + 50);
                }
                else //up
                {
                    this.Cursor = new Cursor(Cursor.Current.Handle);
                    Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - 50);
                }
            };

            tvForm.Show();
        }

        public void UpdateTvForm(TvShow tvShow)
        {
            Form tvForm = null;
            FormCollection formCollection = Application.OpenForms;

            foreach (Form f_ in formCollection)
            {
                if (f_.Text.Equals("tvForm"))
                {
                    tvForm = f_;
                }
            }

            List<Control> toRemove = new List<Control>();
            Panel masterPanel = null;
            Panel mainPanel = null;
            CustomScrollbar customScrollbar = null;
            Label overviewLabel = null;
            Button seasonButton = null;
            Button resumeButton = null;
            Button resetButton = null;
            PictureBox tvShowBackdropBox = null;

            foreach (Control c in tvForm.Controls)
            {
                if (c.Name.Equals("customScrollbar"))
                {
                    customScrollbar = (CustomScrollbar)c;
                }

                Button tempButton = c as Button;
                if (tempButton != null && tempButton.Name.Contains("seasonButton"))
                {
                    seasonButton = tempButton;
                }
                if (tempButton != null && tempButton.Name.Contains("resetButton"))
                {
                    resetButton = tempButton;
                }
                if (tempButton != null && tempButton.Name.Contains("resumeButton"))
                {
                    resumeButton = tempButton;
                }

                //To-do: remove cast and just check names
                Panel p_ = c as Panel;
                if (p_ != null && p_.Name.Equals("tvFormMainPanel"))
                {
                    masterPanel = p_;
                    foreach (Control ctrl in p_.Controls)
                    {
                        Label tempLabel = ctrl as Label;
                        if (tempLabel != null && tempLabel.Name.Equals("overviewLabel"))
                        {
                            overviewLabel = tempLabel;
                        }

                        PictureBox tempBox = ctrl as PictureBox;
                        if (tempBox != null && tempBox.Name.Equals("tvShowBackdropBox"))
                        {
                            tvShowBackdropBox = tempBox;
                        }

                        Panel p = ctrl as Panel;
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
                }
            }

            foreach (Control c in toRemove)
            {
                mainPanel.Controls.Remove(c);
            }

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

            CustomDialog.UpdateScrollBar(customScrollbar, masterPanel);
            seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));

            if (tvShow.LastEpisode == null)
            {
                tvForm.Controls.Remove(resumeButton);
                tvForm.Controls.Remove(resetButton);
            }
            mainPanel.Refresh();
        }

        private List<Control> CreateEpisodePanels(TvShow tvShow)
        {
            Font episodeOverviewFont = new Font("Arial", 12, FontStyle.Regular);
            Font episodeNameFont = new Font("Arial", 14, FontStyle.Bold);
            List<Control> episodePanelList = new List<Control>();
            int index = tvShow.CurrSeason == -1 ? tvShow.Seasons.Length - 1 : tvShow.CurrSeason - 1;
            Season currSeason = tvShow.Seasons[index];
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
                episodeBox.Width = 300;
                //To-do: make const
                episodeBox.Height = (int)(episodeBox.Width / 1.777777777777778);

                if (currEpisode.Backdrop != null)
                {
                    string eImagePath = currEpisode.Backdrop;
                    episodeBox.BackgroundImage = Image.FromFile(eImagePath);
                    episodeBox.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    //To-do: make new no preview for episode box size
                    episodeBox.BackgroundImage = Properties.Resources.noprevSeason;
                    episodeBox.BackgroundImageLayout = ImageLayout.Stretch;
                }
                episodeBox.BackColor = SystemColors.Desktop;
                episodeBox.Cursor = blueHandCursor;
                episodeBox.SizeMode = PictureBoxSizeMode.CenterImage;
                episodeBox.Click += TvShowEpisodeBox_Click;
                episodeBox.Name = currEpisode.Path;

                //To-do: set minimum time for bar to show up
                if (currEpisode.SavedTime != 0)
                {
                    ProgressBar progressBar = PlayerForm.CreateProgressBar(currEpisode.SavedTime, currEpisode.Length);
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
                episodeNameLabel.Font = episodeNameFont;
                episodeNameLabel.AutoSize = true;
                episodeNameLabel.Padding = new Padding(episodeBox.Width + 15, 10, 0, 15);
                //To-do: remove unecessary name tags
                episodeNameLabel.Name = "episodeNameLabel";

                Label episodeOverviewLabel = new Label() { Text = currEpisode.Overview };
                episodeOverviewLabel.Dock = DockStyle.Top;
                episodeOverviewLabel.Font = episodeOverviewFont;
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

        #endregion

        #region Seasons

        private void ResetSeasons(TvShow tvShow, int[] seasonsSelection)
        {
            if (seasonsSelection[0] == 0)
            {
                tvShow.LastEpisode = null;
                for (int j = 0; j < tvShow.Seasons.Length; j++)
                {
                    Season currSeason = tvShow.Seasons[j];
                    for (int k = 0; k < currSeason.Episodes.Length; k++)
                    {
                        Episode currEpisode = currSeason.Episodes[k];
                        currEpisode.SavedTime = 0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < seasonsSelection.Length; i++)
                {
                    int seasonIndex = seasonsSelection[i] - 1;
                    Season currSeason = tvShow.Seasons[seasonIndex];
                    for (int j = 0; j < currSeason.Episodes.Length; j++)
                    {
                        Episode currEpisode = currSeason.Episodes[j];
                        currEpisode.SavedTime = 0;
                    }
                }
            }
            UpdateTvForm(tvShow);
        }

        private void SeasonButton_Click(object sender, EventArgs e)
        {
            seasonFormOpen = true;
            bool indexChange = false;

            Button b = sender as Button;
            string showName = b.Name.Replace("seasonButton_", "");
            TvShow tvShow = GetTvShow(showName);

            int seasonNum;
            seasonNum = b.Text.Contains("Season") ? Int32.Parse(b.Text.Replace("Season ", "")) : seasonNum = tvShow.Seasons.Length - 1;

            Form seasonForm = new Form();
            seasonForm.Width = (int)(this.Width / 2.75);
            seasonForm.Height = (int)(this.Height / 1.1);
            seasonForm.AutoScroll = false;
            seasonForm.StartPosition = FormStartPosition.CenterScreen;
            seasonForm.BackColor = SystemColors.Desktop;
            seasonForm.ForeColor = SystemColors.Control;
            seasonForm.FormBorderStyle = FormBorderStyle.None;
            typeof(Form).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, seasonForm, new object[] { true });

            Panel seasonFormMainPanel = new Panel();
            seasonFormMainPanel.Size = seasonForm.Size;
            seasonFormMainPanel.AutoScroll = true;
            seasonFormMainPanel.Name = "seasonFormMainPanel";
            seasonForm.Controls.Add(seasonFormMainPanel);

            Panel currentPanel = null;
            int count = 0;
            int panelCount = 0;
            int numSeasons = tvShow.Seasons.Length;
            int currSeasonIndex = tvShow.CurrSeason - 1;

            if (tvShow.CurrSeason == -1)
                currSeasonIndex = tvShow.Seasons.Length - 1;

            for (int i = 0; i < numSeasons; i++)
            {
                if (count == 3) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = SystemColors.Desktop;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    panelCount++;
                    seasonFormMainPanel.Controls.Add(currentPanel);
                    seasonFormMainPanel.Controls.SetChildIndex(currentPanel, 0);
                }

                Season currSeason = tvShow.Seasons[i];
                PictureBox seasonBox = new PictureBox();
                seasonBox.Width = numSeasons > 6 ? (int)(seasonForm.Width / 3.14) : (int)(seasonForm.Width / 2.99);
                seasonBox.Height = (int)(seasonBox.Width * 1.5);

                if (currSeason.Poster != null)
                {
                    string imagePath = currSeason.Poster;
                    seasonBox.Image = Image.FromFile(imagePath);
                }
                else if (currSeason.Id == -1)
                {
                    seasonBox.Image = Properties.Resources.extras;
                }
                else
                {
                    seasonBox.Image = Properties.Resources.noprevSeason;
                }

                seasonBox.BackColor = SystemColors.Desktop;
                seasonBox.Left = seasonBox.Width * currentPanel.Controls.Count;
                seasonBox.Cursor = blueHandCursor;
                seasonBox.SizeMode = PictureBoxSizeMode.StretchImage;
                seasonBox.Padding = new Padding(5);
                seasonBox.Name = (i + 1).ToString();

                if (currSeason.Id == -1)
                    seasonBox.Name = "-1";

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
                                    prevSeason.BorderStyle = BorderStyle.None;
                            }
                        }
                    }
                    seasonBox.BorderStyle = BorderStyle.Fixed3D;

                    tvShow.CurrSeason = seasonNum;
                    b.Text = "Season " + seasonNum;

                    if (seasonNum == -1)
                        b.Text = "Extras";

                    seasonForm.Close();
                };

                if (i == currSeasonIndex)
                    seasonBox.BorderStyle = BorderStyle.Fixed3D;

                currentPanel.Controls.Add(seasonBox);
                count++;
            }

            Form tvForm = null;
            FormCollection formCollection = Application.OpenForms;

            foreach (Form f_ in formCollection)
            {
                if (f_.Text.Equals("tvForm"))
                    tvForm = f_;
            }

            seasonDimmerForm.Size = tvForm.Size;
            Fader.FadeInCustom(seasonDimmerForm, Fader.FadeSpeed.Normal, 0.9);
            seasonDimmerForm.Location = tvForm.Location;

            if (numSeasons > 6)
            {
                CustomScrollbar customScrollbar = CustomDialog.CreateScrollBar(seasonFormMainPanel);
                customScrollbar.Scroll += (s, e_) =>
                {
                    seasonFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
                };
                seasonFormMainPanel.MouseWheel += (s, e_) =>
                {
                    int newVal = -seasonFormMainPanel.AutoScrollPosition.Y;
                    customScrollbar.Value = newVal;
                    customScrollbar.Invalidate();
                    Application.DoEvents();
                };
                seasonForm.Controls.Add(customScrollbar);
                customScrollbar.BringToFront();
            }

            seasonForm.ShowDialog();
            seasonForm.Dispose();
            seasonFormOpen = false;
            Fader.FadeOut(seasonDimmerForm, Fader.FadeSpeed.Normal);

            if (indexChange)
                UpdateTvForm(tvShow);
        }

        private TvShow GetTvShow(string name)
        {
            for (int i = 0; i < media.TvShows.Length; i++)
            {
                if (media.TvShows[i].Name.Equals(name))
                    return media.TvShows[i];
            }
            return null;
        }

        public Episode GetTvEpisode(string showName, string episodeName, out int season)
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

        #endregion

        #region Movies

        private void MovieBackdropBox_Click(object sender, EventArgs e)
        {
            PictureBox p = sender as PictureBox;
            string path = p.Name;
            string[] pathSplit = path.Split('\\');
            string movieName = pathSplit[pathSplit.Length - 1].Split('.')[0];
            LaunchVlc(movieName, null, path, null);
        }

        private void MovieBox_Click(object sender, EventArgs e)
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
            movieForm.Name = "movieForm";
            typeof(Form).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, movieForm, new object[] { true });

            RoundButton closeButton = new RoundButton();
            closeButton.BackgroundImage = Properties.Resources.close;
            closeButton.BackgroundImageLayout = ImageLayout.Zoom;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.Red;
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(64, 64);
            closeButton.Click += CloseButton_Click;
            closeButton.Cursor = blueHandCursor;

            movieForm.Controls.Add(closeButton);
            closeButton.Location = new Point(movieForm.Width - (int)(closeButton.Width * 1.165), (closeButton.Width / 8));

            Font headerFont = new Font("Arial", 24, FontStyle.Bold);
            Font overviewFont = new Font("Arial", 12, FontStyle.Regular);

            PictureBox movieBackdropBox = new PictureBox();
            string imagePath = movie.Backdrop;
            movieBackdropBox.BackgroundImage = Image.FromFile(imagePath);
            movieBackdropBox.BackgroundImageLayout = ImageLayout.Stretch;
            movieBackdropBox.BackColor = SystemColors.Desktop;
            movieBackdropBox.Dock = DockStyle.Top;
            movieBackdropBox.Cursor = blueHandCursor;
            movieBackdropBox.Height = (int)(movieForm.Height / 1.777777777777778);
            movieBackdropBox.SizeMode = PictureBoxSizeMode.CenterImage;
            movieBackdropBox.Name = movie.Path;
            movieBackdropBox.Click += MovieBackdropBox_Click;

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
            headerLabel.Font = headerFont;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 0);

            Label overviewLabel = new Label() { Text = movie.Overview };
            overviewLabel.Dock = DockStyle.Top;
            overviewLabel.Font = overviewFont;
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

        private Movie GetMovie(object name)
        {
            for (int i = 0; i < media.Movies.Length; i++)
            {
                if (media.Movies[i].Name.Equals(name))
                    return media.Movies[i];
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
            mainFormMainPanel.MouseWheel += MainFormMainPanel_MouseWheel;

            closeButton.Visible = true;
            closeButton.Location = new Point(mainFormMainPanel.Width - (int)(closeButton.Width * 1.75), (closeButton.Width / 6));
            closeButton.Cursor = blueHandCursor;

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
                    currentPanel.BackColor = SystemColors.Desktop;
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
                movieBox.BackColor = SystemColors.Desktop;
                movieBox.Left = movieBox.Width * currentPanel.Controls.Count;
                movieBox.Cursor = blueHandCursor;
                movieBox.SizeMode = PictureBoxSizeMode.StretchImage;
                movieBox.Padding = new Padding(20);
                movieBox.Name = media.Movies[i].Name;
                movieBox.Click += MovieBox_Click;
                currentPanel.Controls.Add(movieBox);
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
                    currentPanel.BackColor = SystemColors.Desktop;
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
                tvShowBox.Image = Image.FromFile(imagePath);
                tvShowBox.BackColor = SystemColors.Desktop;
                tvShowBox.Left = tvShowBox.Width * currentPanel.Controls.Count;
                tvShowBox.Cursor = blueHandCursor;
                tvShowBox.SizeMode = PictureBoxSizeMode.StretchImage;
                tvShowBox.Padding = new Padding(20);
                tvShowBox.Name = media.TvShows[i].Name;
                tvShowBox.Click += TvShowBox_Click;
                currentPanel.Controls.Add(tvShowBox);
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

        #region Build cache

        private void UpdateLoadingLabel(string text)
        {
            if (text == null)
            {
                loadingLabel.Invoke(new MethodInvoker(delegate
                {
                    loadingLabel.BringToFront();
                }));
            }

            if (loadingLabel.InvokeRequired)
            {
                loadingLabel.Invoke(new MethodInvoker(delegate
                {
                    loadingLabel.Text = text;
                }));
            }
            else
            {
                loadingLabel.Text = text;
            }
        }

        private async Task BuildCacheAsync()
        {
            //To-do: Hover over button change this function fucks up blue-link
            this.UseWaitCursor = true;
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // loop through media... check for identifying item only from api...if not there update
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                for (int i = 0; i < media.Movies.Length; i++)
                {
                    //if id is not 0 expected to be init
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
                        //To-do: Add to gui
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

                    //if id is not 0 expected to be init
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

                        for (int k = 0; k < episodes.Length; k++)
                        {
                            if (episodes[k].Id != 0) continue;
                            if (k > jEpisodes.Count - 1) continue;
                            JObject jEpisode = (JObject)jEpisodes[k];
                            Episode episode = episodes[k];
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
                        }
                        seasonIndex++;
                    }
                }
            }

            Array.Sort(media.Movies, Movie.SortMoviesAlphabetically());
            Array.Sort(media.TvShows, TvShow.SortTvShowsAlphabetically());

            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);
            this.UseWaitCursor = false;
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

            if (moviesDir == null || tvDir == null) throw new ArgumentNullException();

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
                    string[] episodeNameNumber = namePath[namePath.Length - 1].Split('%');
                    int fileSuffixIndex = episodeNameNumber[1].LastIndexOf('.');
                    string episodeName = episodeNameNumber[1].Substring(0, fileSuffixIndex).Trim();
                    Episode episode = new Episode(0, episodeName, episodeEntries[j]);
                    season.Episodes[j] = episode;
                    //TO-DO: catch error when no number % name
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

        #endregion

        #region Background worker

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string mediaPath = ConfigurationManager.AppSettings["mediaPath"];
            string mediaPathB = ConfigurationManager.AppSettings["mediaPathB"];
            ProcessDirectory(mediaPath, mediaPathB);
            
            if (media == null) throw new ArgumentNullException();

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
            InitGui();
            loadingCircle1.Dispose();
            loadingCircle1.SendToBack();
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

        private void UpdateSystemCursor()
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

        private void LaunchVlc(string mediaName, string episodeName, string path, Form tvForm)
        {
            TvShow currTvShow = null;
            Episode currEpisode = null;
            Movie currMovie = null;
            int currSeason = 0;

            if (episodeName != null)
            {
                currTvShow = GetTvShow(mediaName);
                currEpisode = GetTvEpisode(mediaName, episodeName, out currSeason);
            }
            else
            {
                currMovie = GetMovie(mediaName);
            }

            long savedTime = 0;
            int runningTime;

            if (currMovie == null)
            {
                runningTime = currTvShow.RunningTime;
                if (currEpisode != null)
                    savedTime = currEpisode.SavedTime;
            }
            else
            {
                runningTime = currMovie.RunningTime;
            }

            Form playerForm = new PlayerForm(path, savedTime, runningTime, currTvShow, currEpisode, tvForm);
            playerForm.ShowDialog();
            playerForm.Dispose();
            isPlaying = false;
            if (tvForm != null)
                tvForm.Refresh();
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