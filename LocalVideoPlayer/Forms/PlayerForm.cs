using LibVLCSharp.Shared;
using LocalVideoPlayer.Forms;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    public partial class PlayerForm : Form
    {
        public LibVLC libVlc;
        public MediaPlayer mediaPlayer;
        private TvShow currTvShow;
        private Form tvForm;
        private Episode currEpisode;
        private string path;
        private long seekTime;
        private int runningTime;
        private Timer pollingTimer;
        private bool mouseDown = false;
        private bool controlsVisible = false;
        private Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);

        public PlayerForm(string p, long s, int r, TvShow t, Episode ep, Form tf)
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            closeButton.Cursor = blueHandCursor;
            timeline.Cursor = blueHandCursor;
            playButton.Cursor = blueHandCursor;
            videoView1.Cursor = Cursors.Default;

            //some parameters are in tv object > remove
            tvForm = tf;
            currEpisode = ep;
            currTvShow = t;
            seekTime = s;
            path = p;
            runningTime = r;

            long max = runningTime * 60000;
            timeline.Maximum = max;
            timeline.Value = seekTime;

            DirectoryInfo d = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            libVlc = new LibVLC(); // "--verbose=2");
            //libVlc.SetLogFile("vlclog.txt");
            //libVlc.Log += (sender, e) => Console.WriteLine($"[{e.Level}] {e.Module}:{e.Message}");

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
                throw new Exception("VLC error");
            };

            mediaPlayer.EndReached += MediaPlayer_EndReached;

            /*mediaPlayer.Buffering += (sender, e) =>
            {
                //To-do: progress bar or wait cursor
                //cast to MediaPlayer.Event.Buffering -> e.GetBuffering();
            };*/

            pollingTimer = new Timer();
            pollingTimer.Tick += new EventHandler(Polling_Tick);
            pollingTimer.Interval = 10000;
        }

        #region General form functions

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            closeButton.Location = new Point(this.Width - (int)(closeButton.Width * 1.25), (closeButton.Width / 4));
            closeButton.BringToFront();
            playButton.Location = new Point(playButton.Width / 4, this.Height - (int)(playButton.Width * 1.25));
            playButton.BringToFront();
            timeline.Size = new Size(this.Width - (int)(playButton.Width * 3), playButton.Height / 2);
            timeline.Location = new Point(playButton.Width + 20, this.Height - (int)(playButton.Height * 1.025));
            timeLbl.Location = new Point(timeline.Location.X + timeline.Width, timeline.Location.Y);
            timeLbl.BringToFront();

            this.Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(0, this.Height * 2);

            FileInfo media = new FileInfo(path);
            Media currentMedia = CreateMedia(libVlc, path, FromType.FromPath);

            bool result = mediaPlayer.Play(currentMedia);
            //Console.WriteLine("LOAD: " + path);
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

                    if (path.Contains("Extras"))
                    {
                        currEpisode.SavedTime = endTime;
                    }
                    else
                    {
                        currTvShow.CurrSeason = currSeason;
                        currTvShow.LastEpisode = currEpisode;
                        currEpisode.SavedTime = endTime;
                    }
                }

                if (currEpisode.SavedTime > currEpisode.Length)
                {
                    currEpisode.SavedTime = currEpisode.Length;
                }
                UpdateProgressBar();
            }

            mediaPlayer.Dispose();
            libVlc.Dispose();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            if(mediaPlayer.IsPlaying)
            {
                mediaPlayer.Pause();
            }
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
                                    //To-do: if episode name is similar enough wrong progress bar updated
                                    if (ePanel.Name.Contains(currEpisode.Name))
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
                    //https://stackoverflow.com/questions/229554/whats-the-difference-between-invoke-and-begininvoke
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

        #endregion

        #region Media player 

        private Media CreateMedia(LibVLC libVlc, string path, FromType fromPath)
        {
            Media media = new Media(libVlc, path, FromType.FromPath);
            //media.AddOption(":avcodec-hw=none");
            media.AddOption(":avcodec-threads=6");
            media.AddOption(":no-mkv-preload-local-dir");
            return media;
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            if (currTvShow != null)
            {
                currEpisode.SavedTime = currEpisode.Length;
                UpdateProgressBar();

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
                                //To-do: reset progress bars
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
                                    //Console.WriteLine("going from season " + (i) + " to " + (i + 1));
                                    currSeason = currTvShow.Seasons[i + 1];
                                    currTvShow.CurrSeason++;
                                    currEpisode = currSeason.Episodes[0];
                                    path = currEpisode.Path;
                                    timeline.Value = 0;
                                    Media nextMedia = CreateMedia(libVlc, path, FromType.FromPath);
                                    //To-do: log?
                                    //Console.WriteLine("NEXT: " + path);
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
                                    MainForm mainForm = null;
                                    FormCollection formCollection = Application.OpenForms;
                                    foreach (Form f_ in formCollection)
                                    {

                                        if (f_.Name.Equals("MainForm"))
                                        {
                                            mainForm = (MainForm)f_;
                                        }
                                    }
                                    if (mainForm == null) throw new ArgumentNullException();

                                    if (mainForm.InvokeRequired)
                                    {
                                        mainForm.BeginInvoke(new MethodInvoker(delegate { mainForm.UpdateTvForm(currTvShow); }));
                                    }
                                    else
                                    {
                                        mainForm.UpdateTvForm(currTvShow);
                                    }

                                    return;
                                }
                            }
                            else
                            {
                                currEpisode = currSeason.Episodes[j + 1];
                                path = currEpisode.Path;
                                timeline.Value = 0;
                                Media nextMedia = CreateMedia(libVlc, path, FromType.FromPath);
                                //Console.WriteLine("NEXT: " + path);
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

        #endregion

        #region Timeline

        private void Timeline_ValueChanged(object sender, long value)
        {
            string timeString;

            if (mediaPlayer != null)
            {
                TimeSpan lengthTime = TimeSpan.FromMilliseconds(mediaPlayer.Length);
                TimeSpan currTime = TimeSpan.FromMilliseconds(mediaPlayer.Time);

                if (value > 3600000) //hour in ms
                {
                    timeString = currTime.ToString(@"hh\:mm\:ss") + "/" + lengthTime.ToString(@"hh\:mm\:ss");
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

    public class RoundButton : Button
    {
        //https://stackoverflow.com/questions/3708113/round-shaped-buttons
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            GraphicsPath grPath = new GraphicsPath();
            grPath.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new System.Drawing.Region(grPath);
            base.OnPaint(e);
        }
    }
}
