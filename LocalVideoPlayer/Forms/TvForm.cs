﻿using CustomControls;
using LocalVideoPlayer.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    public partial class TvForm : Form
    {
        static private bool seasonFormOpen = false;
        static private bool resetFormOpen = false;
        static private Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);
        static private MRG.Controls.UI.LoadingCircle seasonTransitionCircle;
        static private TreeView dirViewG;
        static private ListView fileViewG;

        public TvForm()
        {
            InitializeComponent();
            InitializeSeasonCircle();
        }

        public static TvShow GetTvShow(string name)
        {
            for (int i = 0; i < MainForm.media.TvShows.Length; i++)
            {
                if (MainForm.media.TvShows[i].Name.Equals(name))
                    return MainForm.media.TvShows[i];
            }
            return null;
        }

        static public Episode GetTvEpisode(string showName, string episodeName, out int season)
        {
            for (int i = 0; i < MainForm.media.TvShows.Length; i++)
            {
                if (MainForm.media.TvShows[i].Name.Equals(showName))
                {
                    for (int j = 0; j < MainForm.media.TvShows[i].Seasons.Length; j++)
                    {
                        Season currSeason = MainForm.media.TvShows[i].Seasons[j];

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

        #region Tv Click Functions

        static public void TvShowEpisodeBox_Click(object sender, EventArgs e)
        {
            PlayerForm.isPlaying = true;
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
            PlayerForm.LaunchVlc(showName, episodeName, path, tvForm);
        }

        static public void TvShowBox_Click(object sender, EventArgs e)
        {
            Form tvForm = new TvForm();

            PictureBox pictureBox = null;
            pictureBox = sender as PictureBox;
            
            TvShow tvShow = GetTvShow(pictureBox.Name);

            tvForm.Name = tvShow.Name;
            tvForm.Width = (int)(MainForm.mainFormSize.Width / 1.75);
            tvForm.Height = MainForm.mainFormSize.Height;

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
            closeButton.Click += MainForm.CloseButton_Click;
            closeButton.Location = new Point(tvForm.Width - (int)(closeButton.Width * 1.65), closeButton.Width / 8);

            Font mainHeaderFont = new Font("Arial", 26, FontStyle.Bold);
            Font episodeHeaderFont = new Font("Arial", 16, FontStyle.Bold);
            Font overviewFont = new Font("Arial", 12, FontStyle.Regular);

            PictureBox tvShowBackdropBox = new PictureBox();
            tvShowBackdropBox.Height = (int)(tvForm.Height / 1.777777777777778);
            string imagePath = tvShow.Backdrop;
            try
            {
                tvShowBackdropBox.Image = Image.FromFile(imagePath);
            }
            catch
            {
                tvShowBackdropBox.Image = Properties.Resources.noprev;
            }
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
                    int[] seasons = CustomDialog.ShowResetSeasons(tvShow.Name, tvShow.Seasons.Length, MainForm.mainFormSize.Width, MainForm.mainFormSize.Height);
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
                    PlayerForm.isPlaying = true;
                    Episode lastEpisode = tvShow.LastEpisode;
                    PlayerForm.LaunchVlc(tvShow.Name, lastEpisode.Name, lastEpisode.Path, tvForm);
                    PlayerForm.isPlaying = false;
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
                if (seasonFormOpen || PlayerForm.isPlaying || resetFormOpen) return;
                tvForm.Close();
                Fader.FadeOut(MainForm.dimmerForm, Fader.FadeSpeed.Normal);
            };

            MainForm.dimmerForm.Size = MainForm.mainFormSize;
            Fader.FadeInCustom(MainForm.dimmerForm, Fader.FadeSpeed.Normal, 0.8);
            MainForm.dimmerForm.Location = MainForm.mainFormLoc;

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
                    Cursor.Current = new Cursor(Cursor.Current.Handle);
                    Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y + 50);
                }
                else //up
                {
                    Cursor.Current = new Cursor(Cursor.Current.Handle);
                    Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - 50);
                }
            };

            tvForm.Show();
        }

        #endregion

        #region Tv Gui

        static public void UpdateTvForm(TvShow tvShow)
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

                        Panel p = ctrl as Panel;
                        if (p != null && p.Name.Equals("mainPanel"))
                        {
                            mainPanel = p;
                            foreach (Control c_ in mainPanel.Controls)
                            {
                                SplitContainer tempContainer = c_ as SplitContainer;
                                if (tempContainer != null)
                                {
                                    masterPanel.Controls.Remove(tempContainer);
                                    tempContainer.Dispose();
                                }

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
                c.Dispose();
            }

            List<Control> episodePanelList = null;
            if (tvShow.CurrSeason == -1)
            {
                SplitContainer extrasContainer = CreateExtrasPicker(tvShow, tvForm);
                extrasContainer.Dock = DockStyle.None;
                mainPanel.Controls.Add(extrasContainer);
                extrasContainer.Location = new Point(seasonButton.Location.X, 120);
                extrasContainer.Width = seasonButton.Width;
                extrasContainer.Height = MainForm.mainFormSize.Height / 2;
                extrasContainer.SplitterDistance = seasonButton.Width / 3;
                extrasContainer.BringToFront();
                DirView_NodeMouseClick(null, null);
            }
            else
            {
                episodePanelList = CreateEpisodePanels(tvShow);
            }

            if (episodePanelList != null)
            {
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

        static private List<Control> CreateEpisodePanels(TvShow tvShow)
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
                    try
                    {
                        episodeBox.BackgroundImage = Image.FromFile(eImagePath);
                    }
                    catch
                    {
                        episodeBox.BackgroundImage = Properties.Resources.noprev;
                    }
                    episodeBox.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    episodeBox.BackgroundImage = Properties.Resources.noprev;
                    episodeBox.BackgroundImageLayout = ImageLayout.Stretch;
                }
                episodeBox.BackColor = SystemColors.Desktop;
                episodeBox.Cursor = blueHandCursor;
                episodeBox.SizeMode = PictureBoxSizeMode.CenterImage;
                episodeBox.Click += TvForm.TvShowEpisodeBox_Click;
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
                episodeOverviewLabel.MaximumSize = new Size((MainForm.mainFormSize.Width / 2) + (episodeBox.Width / 8), MainForm.mainFormSize.Height);
                episodeOverviewLabel.Name = "episodeOverviewLabel";

                //To-do: add running time > update index in for loop
                episodePanel.Controls.Add(episodeBox);
                episodePanel.Controls.Add(episodeOverviewLabel);
                episodePanel.Controls.Add(episodeNameLabel);
            }

            return episodePanelList;
        }

        private void InitializeSeasonCircle()
        {
            seasonTransitionCircle = new MRG.Controls.UI.LoadingCircle();
            seasonTransitionCircle.Active = true;
            seasonTransitionCircle.Color = Color.DarkGray;
            seasonTransitionCircle.InnerCircleRadius = 100;
            seasonTransitionCircle.Location = new Point(232, 40);
            seasonTransitionCircle.Name = "loadingCircle2";
            seasonTransitionCircle.NumberSpoke = 24;
            seasonTransitionCircle.OuterCircleRadius = 160;
            seasonTransitionCircle.RotationSpeed = 100;
            seasonTransitionCircle.Size = new Size(344, 344);
            seasonTransitionCircle.SpokeThickness = 8;
            seasonTransitionCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.MacOSX;
            seasonTransitionCircle.TabIndex = 0;
            seasonTransitionCircle.Text = "loadingCircle2";
            MainForm.seasonDimmerForm.Controls.Add(seasonTransitionCircle);
        }

        #endregion

        #region Seasons

        static private void ResetSeasons(TvShow tvShow, int[] seasonsSelection)
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

        static private void SeasonButton_Click(object sender, EventArgs e)
        {
            seasonFormOpen = true;
            bool indexChange = false;

            Button b = sender as Button;
            string showName = b.Name.Replace("seasonButton_", "");
            TvShow tvShow = TvForm.GetTvShow(showName);

            int seasonNum;
            seasonNum = b.Text.Contains("Season") ? Int32.Parse(b.Text.Replace("Season ", "")) : seasonNum = tvShow.Seasons.Length - 1;

            Form seasonForm = new Form();
            seasonForm.Width = (int)(MainForm.mainFormSize.Width / 2.75);
            seasonForm.Height = (int)(MainForm.mainFormSize.Height / 1.1);
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
                    try
                    {
                        seasonBox.Image = Image.FromFile(imagePath);
                    }
                    catch
                    {
                        seasonBox.Image = Properties.Resources.noprevSeason;
                    }
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

            MainForm.seasonDimmerForm.Size = tvForm.Size;
            Fader.FadeInCustom(MainForm.seasonDimmerForm, Fader.FadeSpeed.Normal, 0.9);
            MainForm.seasonDimmerForm.Location = tvForm.Location;
            seasonTransitionCircle.Location = new Point(MainForm.seasonDimmerForm.Width / 2 - seasonTransitionCircle.Width / 2, MainForm.seasonDimmerForm.Height / 2 - seasonTransitionCircle.Height / 2);

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
            Fader.FadeOut(MainForm.seasonDimmerForm, Fader.FadeSpeed.Normal);

            if (indexChange)
                UpdateTvForm(tvShow);
        }

        #endregion

        #region Extras 

        static private SplitContainer CreateExtrasPicker(TvShow tvShow, Form tvForm)
        {
            #region Initialize picker

            SplitContainer mainContainer = new SplitContainer();
            NoHScrollTree dirView = new NoHScrollTree();
            ListView fileView = new ListView();

            mainContainer.Dock = DockStyle.Fill;
            mainContainer.Panel1.Controls.Add(dirView);
            mainContainer.Panel2.Controls.Add(fileView);
            mainContainer.Name = "extrasContainer";
            dirView.Cursor = blueHandCursor;
            dirView.Dock = DockStyle.Fill;
            dirView.ImageIndex = 0;
            dirView.ImageList = MainForm.imageList1;
            dirView.SelectedImageIndex = 0;
            dirView.NodeMouseClick += DirView_NodeMouseClick;

            /*dirView.MouseWheel += (s, e_) =>
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
            };*/


            ColumnHeader c1 = new ColumnHeader();
            ColumnHeader c2 = new ColumnHeader();
            ColumnHeader c3 = new ColumnHeader();
            c1.Text = "Name";
            c2.Text = "Type";
            c3.Text = "Path";

            fileView.Columns.AddRange(new ColumnHeader[] { c1, c2, c3 });
            fileView.Cursor = blueHandCursor;
            fileView.Dock = DockStyle.Fill;
            fileView.SmallImageList = MainForm.imageList1;
            fileView.UseCompatibleStateImageBehavior = false;
            fileView.View = View.Details;

            dirView.BackColor = SystemColors.Desktop;
            dirView.ForeColor = SystemColors.Control;
            fileView.BackColor = SystemColors.Desktop;
            fileView.ForeColor = SystemColors.Control;
            Font extrasFont = new Font("Arial", 12F, FontStyle.Regular);
            dirView.Font = extrasFont;
            fileView.Font = extrasFont;
            fileView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            fileView.OwnerDraw = true;
            fileView.Scrollable = false;
            //ShowScrollBar(fileView.Handle, (int)SB_VERT, true);

            fileView.DrawColumnHeader += (s, e) =>
            {
                headerDraw(s, e);
            };

            fileView.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
            };

            fileView.ItemSelectionChanged += (sender, e) =>
            {


                if (fileView.SelectedItems.Count == 0)
                    return;

                ListViewItem item = fileView.SelectedItems[0];

                if (item.SubItems[1].Text == "Directory")
                {
                    if (e.IsSelected)
                        e.Item.Selected = false;
                    return;
                }

                string fullPath = item.SubItems[2].Text;
                string[] episodeNameParts = fullPath.Split('\\');
                //string episodeNameFileExt = episodeNameParts[episodeNameParts.Length - 1].Split('%')[1].Trim();
                string episodeName = episodeNameParts[episodeNameParts.Length - 1].Split('.')[0];
                PlayerForm.isPlaying = true;
                PlayerForm.LaunchVlc(null, null, fullPath, null);
            };

            /*fileView.MouseWheel += (s, e_) =>
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
            };*/

            dirViewG = dirView;
            fileViewG = fileView;

            #endregion

            Season extras = tvShow.Seasons[tvShow.Seasons.Length - 1];
            string extrasPath = extras.Episodes[0].Path;
            string[] extrasPathParts = extrasPath.Split('\\');
            extrasPath = "";
            for (int i = 0; i < extrasPathParts.Length; i++)
            {
                extrasPath += extrasPathParts[i] + '\\';
                if (extrasPathParts[i].Contains("Extras")) break;
            }
            PopulateTreeView(dirView, extrasPath);

            return mainContainer;
        }

        static private void headerDraw(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush backBrush = new SolidBrush(SystemColors.Desktop))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            using (SolidBrush foreBrush = new SolidBrush(SystemColors.Control))
            {
                e.Graphics.DrawString(e.Header.Text, e.Font, foreBrush, e.Bounds);
            }
        }

        static private void DirView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected;
            if (sender == null)
            {
                newSelected = dirViewG.Nodes[0];
            }
            else
            {
                newSelected = e.Node;
            }
            fileViewG.Items.Clear();

            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item;

            DirectoryInfo[] dirs = nodeDirInfo.GetDirectories();
            Array.Sort(dirs, delegate (DirectoryInfo d1, DirectoryInfo d2)
            {
                return d1.Name.CompareTo(d2.Name);
            });
            foreach (DirectoryInfo dir in dirs)
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[] { new ListViewItem.ListViewSubItem(item, "Directory"), new ListViewItem.ListViewSubItem(item, dir.FullName) };
                item.SubItems.AddRange(subItems);
                fileViewG.Items.Add(item);
            }

            FileInfo[] files = nodeDirInfo.GetFiles();
            Array.Sort(files, delegate (FileInfo f1, FileInfo f2)
            {
                return f1.Name.CompareTo(f2.Name);
            });
            foreach (FileInfo file in files)
            {
                item = new ListViewItem(file.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[] { new ListViewItem.ListViewSubItem(item, "File"), new ListViewItem.ListViewSubItem(item, file.FullName) };
                item.SubItems.AddRange(subItems);
                fileViewG.Items.Add(item);
            }
            fileViewG.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            int itemsCount = fileViewG.Items.Count;
            int itemHeight = fileViewG.Items[0].Bounds.Height;
            int VisibleItem = (int)fileViewG.ClientRectangle.Height / itemHeight;

            if (itemsCount >= VisibleItem)
            {
                MainForm.ShowScrollBar(fileViewG.Handle, 1, true);
            }
            else
            {
                MainForm.ShowScrollBar(fileViewG.Handle, 1, false); //1 = SB_VERT
            }
        }

        static private void PopulateTreeView(TreeView dirView, string path)
        {
            TreeNode rootNode;
            DirectoryInfo info = new DirectoryInfo(path);
            if (info.Exists)
            {
                rootNode = new TreeNode(info.Name);
                rootNode.Tag = info;
                GetDirectories(info.GetDirectories(), rootNode);
                dirView.Nodes.Add(rootNode);
            }
            dirView.ExpandAll();
        }

        static private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo) {
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode);
                }
                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        #endregion

        #region Movies

        static public Movie GetMovie(object name)
        {
            for (int i = 0; i < MainForm.media.Movies.Length; i++)
            {
                if (MainForm.media.Movies[i].Name.Equals(name))
                    return MainForm.media.Movies[i];
            }
            return null;
        }

        static private void MovieBackdropBox_Click(object sender, EventArgs e)
        {
            PictureBox p = sender as PictureBox;
            string path = p.Name;
            string[] pathSplit = path.Split('\\');
            string movieName = pathSplit[pathSplit.Length - 1].Split('.')[0];
            PlayerForm.LaunchVlc(movieName, null, path, null);
        }

        static public void MovieBox_Click(object sender, EventArgs e)
        {
            Form movieForm = new Form();
            PictureBox p = sender as PictureBox;
            Movie movie = TvForm.GetMovie(p.Name);

            movieForm.Width = (int)(MainForm.mainFormSize.Width / 1.75);
            movieForm.Height = MainForm.mainFormSize.Height;
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
            closeButton.Click += MainForm.CloseButton_Click;
            closeButton.Cursor = blueHandCursor;

            movieForm.Controls.Add(closeButton);
            closeButton.Location = new Point(movieForm.Width - (int)(closeButton.Width * 1.165), (closeButton.Width / 8));

            Font headerFont = new Font("Arial", 24, FontStyle.Bold);
            Font overviewFont = new Font("Arial", 12, FontStyle.Regular);

            PictureBox movieBackdropBox = new PictureBox();
            string imagePath = movie.Backdrop;
            try
            {
                movieBackdropBox.BackgroundImage = Image.FromFile(imagePath);
            }
            catch
            {
                movieBackdropBox.BackgroundImage = Properties.Resources.noprevSeason;
            }
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

            TimeSpan temp = TimeSpan.FromMinutes(movie.RunningTime);
            string hour = temp.Hours > 1 ? "hours " : "hour ";
            Label overviewLabel = new Label() { Text = "Running time: " + temp.Hours + " " + hour + temp.Minutes + " minutes\n\n" + movie.Overview };
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
                if (PlayerForm.isPlaying) return;
                movieForm.Close();
                Fader.FadeOut(MainForm.dimmerForm, Fader.FadeSpeed.Normal);

            };

            MainForm.dimmerForm.Size = MainForm.mainFormSize;
            Fader.FadeInCustom(MainForm.dimmerForm, Fader.FadeSpeed.Normal, 0.8);
            MainForm.dimmerForm.Location = MainForm.mainFormLoc;
            movieForm.Show();
        }

        #endregion

    }
}
