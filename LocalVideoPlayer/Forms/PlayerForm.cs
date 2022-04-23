using LibVLCSharp.Shared;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    public partial class PlayerForm : Form
    {
        static public bool isPlaying = false;
        static private bool subtitles;
        static private int subTrack;
        private bool mouseDown = false;
        private bool controlsVisible = false;
        private bool shrinkTimeLine = false;
        private bool stopPressed = false;
        private int runningTime;
        private long seekTime;
        private string mediaPath;

        private Episode currEpisode;
        private Form tvForm;
        public LibVLC libVlc;
        public MediaPlayer mediaPlayer;
        private TvShow currTvShow;
        private Timer pollingTimer;

        public PlayerForm(string p, long s, int r, bool subs, int st, TvShow t, Episode ep, Form tf)
        {
            if (!DesignMode) Core.Initialize();
            InitializeComponent();
#if DEBUG
            this.WindowState = FormWindowState.Normal;
#endif
            currEpisode = ep;
            currTvShow = t;
            mediaPath = p;
            runningTime = r;
            seekTime = s;
            subtitles = subs;
            subTrack = st;
            tvForm = tf;
            
            long max = runningTime * 60000;
            timeline.Maximum = max;
            timeline.Value = seekTime;

            DirectoryInfo d = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            //libVlc = new LibVLC("--verbose=2"); //libVlc.SetLogFile("vlclog.txt"); //libVlc.Log += (sender, e) => MainForm.Log($"[{e.Level}] {e.Module}:{e.Message}");
            libVlc = new LibVLC();

            pollingTimer = new Timer();
            pollingTimer.Tick += new EventHandler(Polling_Tick);
            pollingTimer.Interval = 3000;

            #region Media player initialize 

            mediaPlayer = new MediaPlayer(libVlc);
            mediaPlayer.EnableMouseInput = false;
            mediaPlayer.EnableKeyInput = false;

            videoView1.MediaPlayer = mediaPlayer;

            mediaPlayer.TimeChanged += (sender, e) =>
            {
                if (!mouseDown && controlsVisible)
                {
                    timeline.Value = mediaPlayer.Time;

                }
            };

            mediaPlayer.LengthChanged += (sender, e) =>
            {
                timeline.Maximum = mediaPlayer.Length;
                if (currTvShow != null)
                {
                    currEpisode.Length = mediaPlayer.Length;
                }
            };

            mediaPlayer.EncounteredError += (sender, e) =>
            {
                MainForm.Log("VLC ERROR: " + e.ToString());
            };

            mediaPlayer.EndReached += MediaPlayer_EndReached;

            #endregion
        }

        #region General form functions

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            closeButton.Location = new Point(this.Width - (int)(closeButton.Width * 1.25), closeButton.Width / 4);
            closeButton.BringToFront();
            playButton.Location = new Point(playButton.Width / 4 - 5, this.Height - (int)(playButton.Width * 1.25));
            playButton.BringToFront();
            timeline.Size = new Size(this.Width - (int)(playButton.Width * 3), playButton.Height / 2);
            timeline.Location = new Point(playButton.Width + 15, this.Height - (int)(playButton.Height * 1.025));
            timeLbl.Location = new Point(timeline.Location.X + timeline.Width, timeline.Location.Y + 1);
            timeLbl.BringToFront();
            MainForm.layoutController.playerFormClose = closeButton;
            MainForm.layoutController.playButton = playButton;

            this.Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(500, this.Height * 4);

            Media currentMedia = CreateMedia(libVlc, mediaPath, FromType.FromPath);
            bool result = mediaPlayer.Play(currentMedia);
            MainForm.Log("Media loaded: " + mediaPath);

            if (seekTime != 0 && result)
            {
                if (seekTime < currEpisode.Length)
                {
                    mediaPlayer.SeekTo(TimeSpan.FromMilliseconds(seekTime));
                }
            }

            if (!result) throw new ArgumentNullException();
        }

        private void PlayerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pollingTimer != null)
            {
                if (pollingTimer.Enabled)
                {
                    pollingTimer.Stop();
                }
                pollingTimer.Dispose();
            }

            if (currTvShow != null)
            {
                int currSeason = 0;
                for (int i = 0; i < currTvShow.Seasons.Length; i++)
                {
                    for (int j = 0; j < currTvShow.Seasons[i].Episodes.Length; j++)
                    {
                        if (currTvShow.Seasons[i].Episodes[j].Name.Equals(currEpisode.Name))
                        {
                            currSeason = i + 1;
                        }
                    }
                }

                long endTime = mediaPlayer.Time;
                if (endTime > 0) //600000 ms = 10 mins 
                {
                    if (currEpisode == null) throw new ArgumentNullException();
                    if (currSeason == 0) throw new ArgumentNullException();

                    if (mediaPath.Contains("Extras"))
                    {
                        currEpisode.SavedTime = endTime;
                    }
                    else
                    {
                        if (currTvShow.LastEpisode == null)
                        {
                            currEpisode.SavedTime = endTime;
                            currTvShow.CurrSeason = currSeason;
                            currTvShow.LastEpisode = currEpisode;
                            MainForm.Log("Last episode null -> " + currTvShow.Name + " " + currTvShow.LastEpisode.Name + ", time: " + currTvShow.LastEpisode.SavedTime + ", season: " + currTvShow.CurrSeason);
                        }
                        else
                        {
                            int lastEpisodeSeason = 0;
                            Episode dummy = TvForm.GetTvEpisode(currTvShow.Name, currTvShow.LastEpisode.Name, out lastEpisodeSeason);
                            MainForm.Log("Prev Last episode: " + currTvShow.Name + " " + currTvShow.LastEpisode.Name + ", time: " + currTvShow.LastEpisode.SavedTime + ", season: " + currTvShow.CurrSeason);

                            currEpisode.SavedTime = endTime;
                            if (lastEpisodeSeason == currSeason)
                            {
                                if (currTvShow.LastEpisode.Id <= currEpisode.Id)
                                {
                                    currTvShow.CurrSeason = currSeason;
                                }
                            }
                            else if (lastEpisodeSeason < currSeason)
                            {
                                currTvShow.CurrSeason = currSeason;
                            }
                            currTvShow.LastEpisode = currEpisode;

                            MainForm.Log("New Last episode: " + currTvShow.Name + " " + currTvShow.LastEpisode.Name + ", time: " + currTvShow.LastEpisode.SavedTime + ", season: " + currTvShow.CurrSeason);
                        }
                    }
                }

                // Update TMDB saved time with actual video length
                if (currEpisode.SavedTime > currEpisode.Length)
                {
                    currEpisode.SavedTime = currEpisode.Length;
                }

                try { UpdateProgressBar(); }
                catch(Exception ex) { MainForm.Log(ex.ToString()); };
            }

            mediaPlayer.Dispose();
            libVlc.Dispose();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            if (mediaPlayer.IsPlaying)
            {
                mediaPlayer.Pause();
            }
            System.Threading.Thread.Sleep(1000);
            this.Close();
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (mediaPlayer.IsPlaying)
            {
                playButton.BackgroundImage = Properties.Resources.pause64;
                mediaPlayer.Pause();
                pollingTimer.Stop();
            }
            else
            {
                playButton.BackgroundImage = Properties.Resources.play64;
                mediaPlayer.Play();
                pollingTimer.Start();
            }

            if (sender == null)
            {
                MouseWorker.DoMouseRightClick();
                MouseWorker.DoMouseRightClick();
            }
        }

        private void Polling_Tick(object sender, EventArgs e)
        {
            timeline.Visible = false;
            timeLbl.Visible = false;
            playButton.Visible = false;
            closeButton.Visible = false;
            controlsVisible = false;
            pollingTimer.Stop();
        }

        private void Control_MouseEnter(object sender, EventArgs e)
        {
            pollingTimer.Stop();

        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            pollingTimer.Start();
        }

        private Form GetForm(string name)
        {
            FormCollection formCollection = Application.OpenForms;
            foreach (Form f_ in formCollection)
            {
                if (f_.Name.Equals(name))
                {
                    return f_;
                }
            }
            MainForm.Log("GetForm null");
            throw new ArgumentNullException();
        }

        #endregion

        #region Progress bar

        static public ProgressBar CreateProgressBar(long savedTime, long length)
        {
            ProgressBar progressBar = new ProgressBar();
            progressBar.Height = 10;
            progressBar.Width = 300;
            progressBar.Maximum = (int)length;
            progressBar.Value = (int)savedTime;
            progressBar.Name = "pBar";
            return progressBar;
        }

        private void UpdateProgressBar()
        {
            try
            {
                Panel episodePanel = null;
                Panel mainPanel = null;
                foreach (Control c in tvForm.Controls)
                {
                    Panel p_ = c as Panel;
                    if (p_ != null && p_.Name.Equals("tvFormMainPanel"))
                    {
                        foreach (Control ctrl in p_.Controls)
                        {
                            Panel p = ctrl as Panel;
                            if (p != null && p.Name.Equals("mainPanel"))
                            {
                                mainPanel = p;
                                foreach (Control c_ in p.Controls)
                                {
                                    Panel ePanel = c_ as Panel;
                                     if (ePanel != null && ePanel.Name.Contains("episodePanel"))
                                    {
                                        string[] ePanelNameTrim = ePanel.Name.Split(new[] { ' ' }, 2);
                                        if (ePanelNameTrim[1].Equals(currEpisode.Name))
                                        {
                                            episodePanel = ePanel;
                                            //To-do: find more break points
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                PictureBox pBox = null;
                foreach (Control c in episodePanel.Controls)
                {
                    pBox = c as PictureBox;
                    if (pBox != null) break;
                }
                if (episodePanel.Controls.Count == 3)
                {
                    ProgressBar progressBar = CreateProgressBar(currEpisode.SavedTime, currEpisode.Length);
                    progressBar.Location = new Point(pBox.Location.X, pBox.Location.Y + pBox.Height);
                    if (episodePanel.InvokeRequired)
                    {
                        // https://stackoverflow.com/questions/229554/whats-the-difference-between-invoke-and-begininvoke
                        episodePanel.Invoke(new MethodInvoker(delegate
                        {
                            episodePanel.Controls.Add(progressBar);
                        }));
                    }
                    else
                    {
                        episodePanel.Controls.Add(progressBar);
                    }
                }
                else if (episodePanel.Controls.Count == 4)
                {
                    ProgressBar progressBar;
                    foreach (Control c in episodePanel.Controls)
                    {
                        if (c.Name.Equals("pBar"))
                        {
                            progressBar = c as ProgressBar;
                            if (progressBar.InvokeRequired)
                            {
                                progressBar.Invoke(new MethodInvoker(delegate
                                {
                                    progressBar.Value = (int)currEpisode.SavedTime;
                                    progressBar.Update();
                                }));
                            }
                            else
                            {
                                progressBar.Value = (int)currEpisode.SavedTime;
                                progressBar.Update();
                            }
                        }
                    }
                }
                if (mainPanel.InvokeRequired)
                {
                    mainPanel.Invoke(new MethodInvoker(delegate
                    {
                        mainPanel.Refresh();
                    }));
                }
                else
                {
                    mainPanel.Refresh();
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                MessageBox.Show("UpdateProgressBar: ", ex.Message);
                MainForm.Log("UpdateProgressBar: " + ex.Message);
            }
        }

        #endregion

        #region Media player 

        static public void LaunchVlc(string mediaName, string episodeName, string path, Form tvForm)
        {
            TvShow currTvShow = null;
            Episode currEpisode = null;
            Movie currMovie = null;
            int currSeason = 0;

            if (episodeName != null)
            {
                currTvShow = TvForm.GetTvShow(mediaName);
                currEpisode = TvForm.GetTvEpisode(mediaName, episodeName, out currSeason);
            }
            else
            {
                currMovie = TvForm.GetMovie(mediaName);
            }

            long savedTime = 0;
            int runningTime = 0;

            if (currTvShow != null)
            {
                runningTime = currTvShow.RunningTime;
                if (currEpisode != null)
                {
                    savedTime = currEpisode.SavedTime;
                }
            }
            else if (currMovie != null)
            {
                runningTime = currMovie.RunningTime;
                subtitles = currMovie.Subtitles;
                subTrack = currMovie.SubtitleTrack;
            }

            Form playerForm = new PlayerForm(path, savedTime, runningTime, subtitles, subTrack, currTvShow, currEpisode, tvForm);
            MainForm.layoutController.Select("playerForm");
            playerForm.ShowDialog();
            playerForm.Dispose();

            isPlaying = false;
            if (tvForm != null) tvForm.Refresh();
        }

        private Media CreateMedia(LibVLC libVlc, string path, FromType fromPath)
        {
            // Add application and vlc .exe to Graphics Settings with High Performance NVIDIA GPU preference
            Media media = new Media(libVlc, path, FromType.FromPath);
            media.AddOption(":avcodec-hw=auto");
            string subtitleTrackOption;
            if (!subtitles)
            {
                subtitleTrackOption = String.Format(":sub-track-id={0}", Int32.MaxValue);
            } 
            else
            {
                subtitleTrackOption = String.Format(":sub-track-id={0}", subTrack);
            }
            media.AddOption(subtitleTrackOption);
            media.AddOption(":no-mkv-preload-local-dir");
            return media;
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            if (currTvShow != null)
            {
                currEpisode.SavedTime = currEpisode.Length;

                try { UpdateProgressBar(); }
                catch (Exception ex) { MainForm.Log(ex.ToString()); };

                if (currEpisode.Id == -1)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate { this.Dispose(); }));
                    }
                    else
                    {
                        this.Dispose();
                    }
                    return;
                }

                for (int i = 0; i < currTvShow.Seasons.Length; i++)
                {
                    Season currSeason = currTvShow.Seasons[i];
                    for (int j = 0; j < currSeason.Episodes.Length; j++)
                    {
                        if (currEpisode.Name.Equals(currSeason.Episodes[j].Name))
                        {
                            if (j == currSeason.Episodes.Length - 1)
                            {
                                if ((i == currTvShow.Seasons.Length - 2 && currTvShow.Seasons[currTvShow.Seasons.Length - 1].Id == -1) ||
                                    i == currTvShow.Seasons.Length - 1)
                                {
                                    if (this.InvokeRequired)
                                    {
                                        this.Invoke(new MethodInvoker(delegate { this.Dispose(); }));
                                    }
                                    else
                                    {
                                        this.Dispose();
                                    }
                                    return;
                                }
                                else
                                {
                                    MainForm.Log(currTvShow.Name + " season change from " + (i) + " to " + (i + 1));
                                    currSeason = currTvShow.Seasons[i + 1];
                                    currTvShow.CurrSeason++;
                                    currEpisode = currSeason.Episodes[0];
                                    mediaPath = currEpisode.Path;
                                    timeline.Value = 0;
                                    Media nextMedia = CreateMedia(libVlc, mediaPath, FromType.FromPath);
                                    MainForm.Log("Media loaded: " + mediaPath);
                                    System.Threading.ThreadPool.QueueUserWorkItem(_ => mediaPlayer.Play(nextMedia));

                                    foreach (Control c in tvForm.Controls)
                                    {
                                        Button b = c as Button;
                                        if (b != null && b.Text.Contains("Season"))
                                        {
                                            if (b.InvokeRequired)
                                            {
                                                b.Invoke(new MethodInvoker(delegate { b.Text = "Season " + currTvShow.CurrSeason; }));
                                            }
                                            else
                                            {
                                                b.Text = "Season " + currTvShow.CurrSeason;
                                            }
                                        }
                                    }
                                    MainForm mainForm = (MainForm)GetForm("MainForm");
                                    if (mainForm == null) throw new ArgumentNullException();

                                    if (mainForm.InvokeRequired)
                                    {
                                        mainForm.BeginInvoke(new MethodInvoker(delegate { TvForm.UpdateTvForm(currTvShow); }));
                                    }
                                    else
                                    {
                                        TvForm.UpdateTvForm(currTvShow);
                                    }

                                    return;
                                }
                            }
                            else
                            {
                                currEpisode = currSeason.Episodes[j + 1];
                                mediaPath = currEpisode.Path;
                                timeline.Value = 0;
                                Media nextMedia = CreateMedia(libVlc, mediaPath, FromType.FromPath);
                                MainForm.Log("Media loaded: " + mediaPath);
                                System.Threading.ThreadPool.QueueUserWorkItem(_ => mediaPlayer.Play(nextMedia));
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate { this.Dispose(); }));
                }
                else
                {
                    this.Dispose();
                }
            }
        }

        private void VideoView1_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = PointToClient(Cursor.Position);
            if (p.Y < this.Height - 50)
            {
                if (!pollingTimer.Enabled)
                {
                    pollingTimer.Enabled = true;
                    pollingTimer.Start();
                }

                playButton.Visible = true;
                closeButton.Visible = true;
                timeline.Visible = true;
                timeLbl.Visible = true;
                controlsVisible = true;
            }
        }

        private void videoView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                MainForm.Log("Space bar pause");
                InitiatePause();
            }
        }

        public void InitiatePause()
        {
            this.Invoke(new MethodInvoker(delegate
            {
                this.Cursor = new Cursor(Cursor.Current.Handle);
                if (!stopPressed)
                {
                    //MouseWorker.DoMouseRightClick();
                    //MouseWorker.DoMouseClick();
                    Cursor.Position = new Point(65, this.Height - 65);
                    stopPressed = true;
                }
                else
                {
                    Cursor.Position = new Point(500, this.Height * 4);
                    //MouseWorker.DoMouseRightClick();
                    //MouseWorker.DoMouseClick();
                    if (!pollingTimer.Enabled)
                    {
                        pollingTimer.Enabled = true;
                        pollingTimer.Start();
                    }
                    stopPressed = false;
                }
            }));

            PlayButton_Click(null, null);
        }

        #endregion

        #region Timeline
        private void Timeline_ValueChanged(object sender, long value)
        {
            string timeString;

            if (mediaPlayer != null)
            {
                TimeSpan lengthTime = TimeSpan.FromMilliseconds(mediaPlayer.Length);
                TimeSpan currTime = TimeSpan.FromMilliseconds(mediaPlayer.Time);

                if (lengthTime.TotalMilliseconds > 3600000) //hour in ms
                {
                    timeString = currTime.ToString(@"hh\:mm\:ss") + "/" + lengthTime.ToString(@"hh\:mm\:ss");
                    if (!shrinkTimeLine)
                    {
                        timeline.Invoke(new MethodInvoker(delegate
                        {

                            timeline.Size = new Size(this.Width - (int)(playButton.Width * 3) - 40, playButton.Height / 2);
                        }));
                        timeLbl.Invoke(new MethodInvoker(delegate
                        {
                            timeLbl.Location = new Point(timeline.Location.X + timeline.Width, timeline.Location.Y);
                        }));
                        shrinkTimeLine = true;
                    }
                }
                else
                {
                    timeString = currTime.ToString(@"mm\:ss") + "/" + lengthTime.ToString(@"mm\:ss");
                }

                timeLbl.Invoke(new MethodInvoker(delegate
                {
                    timeLbl.Text = timeString;
                }));

                if (mouseDown)
                {

                    TimeSpan seekTime = TimeSpan.FromMilliseconds(value);
                    if (seekTime.TotalMilliseconds > lengthTime.TotalMilliseconds)
                    {
                        timeline.Value = (long)lengthTime.TotalMilliseconds;
                    }
                    mediaPlayer.SeekTo(seekTime);
                }
            }

        }

        private void Timeline_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            TimeSpan ts = TimeSpan.FromMilliseconds((double)timeline.Value);
            mediaPlayer.SeekTo(ts);
        }

        private void Timeline_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
        }

        #endregion
    }
}
