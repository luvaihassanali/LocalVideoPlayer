using CustomControls;
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
        static private MRG.Controls.UI.LoadingCircle seasonTransitionCircle;
        static private ImageList imageList1;
        static private ListView fileViewG;
        static private TreeView dirViewG;

        public TvForm()
        {
            InitializeComponent();
            //InitializeSeasonCircle();
            InitializeImageList();
        }

        private void InitializeImageList()
        {
            imageList1 = new ImageList();
            imageList1.Images.Add(Properties.Resources.folder_icon);
            imageList1.Images.Add(Properties.Resources.media_icon);
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up)
            {
                MainForm.layoutController.MovePointPosition(MainForm.layoutController.up);
                return true;
            }
            if (keyData == Keys.Down)
            {
                MainForm.layoutController.MovePointPosition(MainForm.layoutController.down);
                return true;
            }
            if (keyData == Keys.Left)
            {
                MainForm.layoutController.MovePointPosition(MainForm.layoutController.left);
                return true;
            }
            if (keyData == Keys.Right)
            {
                MainForm.layoutController.MovePointPosition(MainForm.layoutController.right);
                return true;
            }
            if (keyData == Keys.Enter)
            {
                MouseWorker.DoMouseClick();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                MainForm.layoutController.CloseCurrentForm();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #region Tv Click Functions

        static public void TvShowEpisodeBox_Click(object sender, EventArgs e)
        {
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
            MainForm.ShowLoadingCursor();
            TvForm tvForm = new TvForm();
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
            MainForm.layoutController.tvFormMainPanel = tvFormMainPanel;

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
            MainForm.layoutController.tvFormClose = closeButton;

            Font mainHeaderFont = new Font("Arial", 26, FontStyle.Bold);
            Font episodeHeaderFont = new Font("Arial", 16, FontStyle.Bold);
            Font overviewFont = new Font("Arial", 12, FontStyle.Regular);

            PictureBox tvShowBackdropBox = new PictureBox();
            tvShowBackdropBox.Height = (int)(tvForm.Height / 1.777777777777778);
            string imagePath = tvShow.Backdrop;
            try
            {
                tvShowBackdropBox.BackgroundImage = Image.FromFile(imagePath);
            }
            catch
            {
                tvShowBackdropBox.BackgroundImage = Properties.Resources.noprev;
            }
            tvShowBackdropBox.BackgroundImageLayout = ImageLayout.Stretch;
            tvShowBackdropBox.SizeMode = PictureBoxSizeMode.CenterImage;
            tvShowBackdropBox.Dock = DockStyle.Top;
            tvShowBackdropBox.Name = "tvShowBackdropBox";
            tvShowBackdropBox.Cursor = MainForm.blueHandCursor;
            tvShowBackdropBox.MouseEnter += (s, ev) =>
            {
                tvShowBackdropBox.Image = Properties.Resources.play;
            };
            tvShowBackdropBox.MouseLeave += (s, ev) =>
            {
                tvShowBackdropBox.Image = null;
            };
            MainForm.layoutController.tvFormControlList.Add(tvShowBackdropBox);

            Button resetButton = null;
            foreach (Control c in tvForm.Controls)
            {
                if (c.Name.Equals("resetButton"))
                    resetButton = (Button)c;
            }

            resetButton.BringToFront();
            resetButton.Padding = new Padding(1, 0, 0, 0);
            resetButton.AutoSize = true;
            resetButton.Location = new Point(tvForm.Width - resetButton.Width - 35, tvShowBackdropBox.Location.Y + tvShowBackdropBox.Height - resetButton.Height - 5);
            resetButton.Cursor = MainForm.blueHandCursor;
            resetButton.Click += (s, e_) =>
            {
                resetFormOpen = true;
                int[] seasons = CustomDialog.ShowResetSeasons(tvShow, MainForm.mainFormSize.Width, MainForm.mainFormSize.Height);
                if (seasons.Length != 1)
                {
                    tvForm.ResetSeasons(tvShow, seasons);
                }
                resetFormOpen = false;
                resetButton.Visible = false;
            };

            if (tvShow.LastEpisode != null)
            {
                int currSeason = 0;
                Episode ep = GetTvEpisode(tvShow.Name, tvShow.LastEpisode.Name, out currSeason);
                tvShow.CurrSeason = currSeason;
            }

            tvShowBackdropBox.Click += (s, e_) =>
            {
                Episode currEpisode = tvShow.LastEpisode == null ? tvShow.Seasons[0].Episodes[0] : tvShow.LastEpisode;
                PlayerForm.LaunchVlc(tvShow.Name, currEpisode.Name, currEpisode.Path, tvForm);
            };

            Panel mainPanel = new Panel();
            mainPanel.BackColor = Color.Black;
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
            headerLabel.Click += (s, e_) =>
            {
                int newVal = -tvFormMainPanel.AutoScrollPosition.Y;
                if (newVal == 0)
                {
                    resetButton.Visible = true;
                }
            };

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
                if (seasonFormOpen || PlayerForm.isPlaying || resetFormOpen)
                {
                    return;
                }
                tvForm.Close();
                Fader.FadeOut(MainForm.dimmerForm, Fader.FadeSpeed.Normal);
                MainForm.layoutController.DeactivateTvForm();
                //System.Threading.Tasks.Task.Run(() => MainForm.SaveMedia());
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
                tvFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
                if (customScrollbar.Value == 0)
                {
                    closeButton.Visible = true;
                }
                else
                {
                    closeButton.Visible = false;
                    if (resetButton != null)
                    {
                        resetButton.Visible = false;
                    }
                }
            };

            tvFormMainPanel.MouseWheel += (s, e_) =>
            {
                tvFormMainPanel.Focus();
                seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));
                int newVal = -tvFormMainPanel.AutoScrollPosition.Y;

                if (newVal == 0)
                {
                    closeButton.Visible = true;
                }
                else
                {
                    closeButton.Visible = false;
                    if (resetButton != null)
                    {
                        resetButton.Visible = false;
                    }
                }
                customScrollbar.Value = newVal;
            };

            tvForm.Controls.Add(customScrollbar);
            customScrollbar.BringToFront();
            MainForm.layoutController.tvScrollbar = customScrollbar;

            foreach (Control c in tvForm.Controls)
            {
                if (c.Name.Equals("seasonButton"))
                {
                    seasonButton = (Button)c;
                }
            }

            if (tvShow.CurrSeason == -1)
            {
                seasonButton.Text = "Extras";
            }

            seasonButton.Name = "seasonButton_" + tvShow.Name;
            seasonButton.Text = "Season " + tvShow.CurrSeason;
            seasonButton.Font = episodeHeaderFont;
            seasonButton.Visible = true;
            seasonButton.Click += SeasonButton_Click;
            seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));
            seasonButton.Size = new Size(episodePanelList[0].Width - 18, seasonButton.Height);
            seasonButton.Cursor = MainForm.blueHandCursor;
            MainForm.layoutController.tvFormControlList.Insert(1, overviewLabel);
            MainForm.layoutController.tvFormControlList.Insert(2, seasonButton);
            tvForm.Show();
            MainForm.layoutController.Select(tvShow.Name);
            MainForm.HideLoadingCursor();
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
            MainForm.layoutController.tvFormControlList.RemoveRange(3, MainForm.layoutController.tvFormControlList.Count - 3);

            List<Control> episodePanelList = null;
            if (tvShow.CurrSeason == -1)
            {
                SplitContainer extrasContainer = CreateExtrasPicker(tvShow, tvForm);
                extrasContainer.Dock = DockStyle.None;
                mainPanel.Controls.Add(extrasContainer);
                extrasContainer.Location = new Point(seasonButton.Location.X, 120);
                extrasContainer.Width = seasonButton.Width;
                extrasContainer.Height = MainForm.mainFormSize.Height / 3;
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
                }
            }

            CustomDialog.UpdateScrollBar(customScrollbar, masterPanel);
            MainForm.layoutController.tvScrollbar = customScrollbar;
            seasonButton.Location = new Point(overviewLabel.Location.X + 20, overviewLabel.Location.Y + overviewLabel.Height + (int)(seasonButton.Height * 1.75));
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
                episodeBox.BackColor = Color.Black;
                episodeBox.Cursor = MainForm.blueHandCursor;
                episodeBox.SizeMode = PictureBoxSizeMode.CenterImage;
                episodeBox.Click += TvShowEpisodeBox_Click;
                episodeBox.Name = currEpisode.Path;

                if (currEpisode.SavedTime != 0)
                {
                    ProgressBar progressBar;
                    if (currEpisode.Length == 0)
                    {
                        progressBar = PlayerForm.CreateProgressBar(currEpisode.SavedTime, tvShow.RunningTime * 60000);
                    }
                    else
                    {
                        progressBar = PlayerForm.CreateProgressBar(currEpisode.SavedTime, currEpisode.Length);
                    }
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
                if (episodeNameLabel.Text.Contains("#"))
                {
                    episodeNameLabel.Text = episodeNameLabel.Text.Replace("#", " && " + Environment.NewLine);
                }

                episodeNameLabel.Dock = DockStyle.Top;
                episodeNameLabel.Font = episodeNameFont;
                episodeNameLabel.AutoSize = true;
                episodeNameLabel.Padding = new Padding(episodeBox.Width + 15, 10, 0, 15);
                episodeNameLabel.Name = "episodeNameLabel";

                Label episodeOverviewLabel = new Label() { Text = currEpisode.Overview };
                episodeOverviewLabel.Dock = DockStyle.Top;
                episodeOverviewLabel.Font = episodeOverviewFont;
                episodeOverviewLabel.AutoSize = true;
                episodeOverviewLabel.Padding = new Padding(episodeBox.Width + 15, 10, 0, 10);
                episodeOverviewLabel.ForeColor = Color.LightGray;
                episodeOverviewLabel.MaximumSize = new Size((MainForm.mainFormSize.Width / 2) + (episodeBox.Width / 8), MainForm.mainFormSize.Height);
                episodeOverviewLabel.Name = "episodeOverviewLabel";

                episodePanel.Controls.Add(episodeBox);
                episodePanel.Controls.Add(episodeOverviewLabel);
                episodePanel.Controls.Add(episodeNameLabel);
                MainForm.layoutController.tvFormControlList.Add(episodeBox);
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

        public void ResetSeasons(TvShow tvShow, int[] seasonsSelection)
        {
            bool fill = false;
            if (seasonsSelection[0] == 1)
            {
                fill = true;
            }
            if (seasonsSelection[1] == 0)
            {
                tvShow.LastEpisode = null;
                for (int j = 0; j < tvShow.Seasons.Length; j++)
                {
                    Season currSeason = tvShow.Seasons[j];
                    for (int k = 0; k < currSeason.Episodes.Length; k++)
                    {
                        Episode currEpisode = currSeason.Episodes[k];
                        if (fill)
                        {
                            currEpisode.SavedTime = tvShow.RunningTime * 60000;
                        }
                        else
                        {
                            currEpisode.SavedTime = 0;
                        }
                    }
                }
                tvShow.CurrSeason = 1;
                tvShow.LastEpisode = null;
            }
            else
            {
                for (int i = 1; i < seasonsSelection.Length; i++)
                {
                    int seasonIndex = seasonsSelection[i] - 1;
                    Season currSeason = tvShow.Seasons[seasonIndex];
                    for (int j = 0; j < currSeason.Episodes.Length; j++)
                    {
                        Episode currEpisode = currSeason.Episodes[j];
                        if (fill)
                        {
                            if (currEpisode.Length != 0)
                            {
                                currEpisode.SavedTime = currEpisode.Length;
                            }
                            else
                            {
                                currEpisode.SavedTime = tvShow.RunningTime * 60000;
                            }
                        }
                        else
                        {
                            currEpisode.SavedTime = 0;
                        }
                    }
                }
                tvShow.CurrSeason = fill ? seasonsSelection[1] + 1 : seasonsSelection[seasonsSelection.Length - 1];
                tvShow.LastEpisode = fill ? tvShow.Seasons[tvShow.CurrSeason - 1].Episodes[0] : tvShow.Seasons[tvShow.CurrSeason - 1].Episodes[0];
            }
            seasonButton.Text = "Season " + tvShow.CurrSeason;
            UpdateTvForm(tvShow);
        }

        static private void SeasonButton_Click(object sender, EventArgs e)
        {

            MainForm.ShowLoadingCursor();
            seasonFormOpen = true;
            bool indexChange = false;
            Button b = sender as Button;
            string showName = b.Name.Replace("seasonButton_", "");
            TvShow tvShow = GetTvShow(showName);
            int seasonNum;
            seasonNum = b.Text.Contains("Season") ? Int32.Parse(b.Text.Replace("Season ", "")) : seasonNum = tvShow.Seasons.Length - 1;

            Form seasonForm = new Form();
            seasonForm.Width = (int)(MainForm.mainFormSize.Width / 2.75);
            seasonForm.Height = (int)(MainForm.mainFormSize.Height / 1.1);
            seasonForm.AutoScroll = false;
            seasonForm.StartPosition = FormStartPosition.CenterScreen;
            seasonForm.BackColor = Color.Black;
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

            if (tvShow.CurrSeason == -1) currSeasonIndex = tvShow.Seasons.Length - 1;

            for (int i = 0; i < numSeasons; i++)
            {
                if (count == 3) count = 0;
                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Black;
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

                seasonBox.BackColor = Color.Black;
                seasonBox.Left = seasonBox.Width * currentPanel.Controls.Count;
                seasonBox.Cursor = MainForm.blueHandCursor;
                seasonBox.SizeMode = PictureBoxSizeMode.StretchImage;
                seasonBox.Padding = new Padding(5);
                seasonBox.Name = (i + 1).ToString();

                if (currSeason.Id == -1)
                    seasonBox.Name = "-1";
                seasonBox.MouseEnter += (s, ev) =>
                {
                    MainForm.layoutController.ClearSeasonBoxBorder();
                    seasonBox.BorderStyle = BorderStyle.Fixed3D;
                };
                seasonBox.MouseLeave += (s, ev) =>
                {
                    seasonBox.BorderStyle = BorderStyle.None;
                };
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
                    if (seasonNum == -1) b.Text = "Extras";

                    seasonForm.Close();
                };

                if (i == currSeasonIndex)
                {
                    seasonBox.BorderStyle = BorderStyle.Fixed3D;
                }

                currentPanel.Controls.Add(seasonBox);
                MainForm.layoutController.seasonFormControlList.Add(seasonBox);
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
            //seasonTransitionCircle.Location = new Point(MainForm.seasonDimmerForm.Width / 2 - seasonTransitionCircle.Width / 2, MainForm.seasonDimmerForm.Height / 2 - seasonTransitionCircle.Height / 2);

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
                };
                seasonForm.Controls.Add(customScrollbar);
                customScrollbar.BringToFront();
                MainForm.layoutController.seasonScrollbar = customScrollbar;
            }

            seasonForm.Shown += (s, e_) =>
            {
                MainForm.layoutController.seasonFormMainPanel = seasonFormMainPanel;
                MainForm.layoutController.seasonFormIndex = currSeasonIndex;
                MainForm.layoutController.Select("seasonButton");
            };
            MainForm.HideLoadingCursor();

            seasonForm.ShowDialog();
            seasonForm.Dispose();
            seasonFormOpen = false;
            Fader.FadeOut(MainForm.seasonDimmerForm, Fader.FadeSpeed.Normal);

            if (indexChange)
            {
                MainForm.ShowLoadingCursor();
                UpdateTvForm(tvShow);
                MainForm.HideLoadingCursor();
            }
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

            dirView.Cursor = MainForm.blueHandCursor;
            dirView.Dock = DockStyle.Fill;
            dirView.ImageIndex = 0;
            dirView.ImageList = imageList1;
            dirView.SelectedImageIndex = 0;
            dirView.NodeMouseClick += DirView_NodeMouseClick;

            ColumnHeader c1 = new ColumnHeader();
            ColumnHeader c2 = new ColumnHeader();
            ColumnHeader c3 = new ColumnHeader();
            c1.Text = "Name";
            c2.Text = "Type";
            c3.Text = "Path";

            fileView.Columns.AddRange(new ColumnHeader[] { c1, c2, c3 });
            fileView.Cursor = MainForm.blueHandCursor;
            fileView.Dock = DockStyle.Fill;
            fileView.SmallImageList = imageList1;
            fileView.UseCompatibleStateImageBehavior = false;
            fileView.View = View.Details;

            dirView.BackColor = Color.Black;
            dirView.ForeColor = SystemColors.Control;
            fileView.BackColor = Color.Black;
            fileView.ForeColor = SystemColors.Control;
            Font extrasFont = new Font("Arial", 12F, FontStyle.Regular);
            dirView.Font = extrasFont;
            fileView.Font = extrasFont;
            fileView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            fileView.OwnerDraw = true;
            fileView.Scrollable = false;

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
                string episodeName = episodeNameParts[episodeNameParts.Length - 1].Split('.')[0];
                PlayerForm.LaunchVlc(null, null, fullPath, null);
            };

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

        static private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
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
            MainForm.ShowLoadingCursor();
            Form movieForm = new Form();
            PictureBox p = sender as PictureBox;
            Movie movie = TvForm.GetMovie(p.Name);

            movieForm.Width = (int)(MainForm.mainFormSize.Width / 1.75);
            movieForm.Height = MainForm.mainFormSize.Height;
            movieForm.AutoScroll = true;
            movieForm.FormBorderStyle = FormBorderStyle.None;
            movieForm.StartPosition = FormStartPosition.CenterScreen;
            movieForm.BackColor = Color.Black;
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
            closeButton.Cursor = MainForm.blueHandCursor;

            movieForm.Controls.Add(closeButton);
            closeButton.Location = new Point(movieForm.Width - (int)(closeButton.Width * 1.165), (closeButton.Width / 8));
            MainForm.layoutController.movieFormClose = closeButton;

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
            movieBackdropBox.BackColor = Color.Black;
            movieBackdropBox.Dock = DockStyle.Top;
            movieBackdropBox.Cursor = MainForm.blueHandCursor;
            movieBackdropBox.Height = (int)(movieForm.Height / 1.777777777777778);
            movieBackdropBox.SizeMode = PictureBoxSizeMode.CenterImage;
            movieBackdropBox.Name = movie.Path;
            movieBackdropBox.Cursor = MainForm.blueHandCursor;
            movieBackdropBox.Click += MovieBackdropBox_Click;

            movieBackdropBox.MouseEnter += (s, ev) =>
            {
                movieBackdropBox.Image = Properties.Resources.play;
            };
            movieBackdropBox.MouseLeave += (s, ev) =>
            {
                movieBackdropBox.Image = null;
            };
            MainForm.layoutController.movieBackropBox = movieBackdropBox;

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
            MainForm.layoutController.Select(movie.Name);
            MainForm.HideLoadingCursor();
        }

        #endregion

        internal static void PlayRandomCartoon()
        {
            for (int i = 0; i < MainForm.cartoonLimit; i++)
            {

                Episode e = GetRandomEpisode();
                MainForm.cartoonShuffleList.Add(e);
            }

            Episode rndEpisode = MainForm.cartoonShuffleList[MainForm.cartoonIndex];
            string path = rndEpisode.Path;
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
            PlayerForm.LaunchVlc(showName, episodeName, path, null);
        }

        internal static Random rnd = new Random();
        internal static Episode GetRandomEpisode()
        {
            Episode rndEpisode;
            int rndVal = rnd.Next(MainForm.cartoons.Count);
            TvShow rndShow = MainForm.cartoons[rndVal];
            rndVal = rnd.Next(rndShow.Seasons.Length);
            Season rndSeason = rndShow.Seasons[rndVal];
            rndVal = rnd.Next(rndSeason.Episodes.Length);
            rndEpisode = rndSeason.Episodes[rndVal];
            return rndEpisode;
        }
    }
}
