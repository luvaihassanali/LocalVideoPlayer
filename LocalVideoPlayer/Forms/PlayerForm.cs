using LibVLCSharp.Shared;
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
        private bool timePanelActive = false;
        private Panel timePanel = null;
        private Label timeLabel = null;

        public PlayerForm(string p, long s, int r, TvShow t, Episode ep, Form tf)
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();

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
            libVlc = new LibVLC();
            mediaPlayer = new MediaPlayer(libVlc);

            mediaPlayer.EnableMouseInput = false;
            mediaPlayer.EnableKeyInput = false;

            //To-do: button pop up on hover? instead of color
            videoView1.MediaPlayer = mediaPlayer;

            mediaPlayer.TimeChanged += (sender, e) =>
            {
                if (!mouseDown)
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
                //To-do: error dialog
                throw new Exception("VLC error");
            };

            mediaPlayer.EndReached += mediaPlayer_EndReached;

            /*mediaPlayer.Buffering += (sender, e) =>
            {
                
            };*/

            pollingTimer = new Timer();
            pollingTimer.Tick += new EventHandler(polling_Tick);
            pollingTimer.Interval = 2000;
        }

        #region General form functions

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            closeButton.Location = new Point(this.Width - (int)(closeButton.Width * 1.25), (closeButton.Width / 4));
            playButton.Location = new Point(playButton.Width / 4, this.Height - (int)(playButton.Width * 1.25));
            timeline.Size = new Size(this.Width - (int)(playButton.Width * 4), playButton.Height / 2);
            timeline.Location = new Point(playButton.Width * 2, this.Height - playButton.Height);


            this.Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(0, this.Height * 2);
            this.Cursor = Cursors.Default;

            FileInfo media = new FileInfo(path);
            Media currentMedia = new Media(libVlc, path, FromType.FromPath);
            bool result = mediaPlayer.Play(currentMedia);
            Console.WriteLine("LOAD: " + path);
            if (seekTime != 0 && result)
            {
                if (seekTime < mediaPlayer.Length)
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

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void playButton_Click(object sender, EventArgs e)
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

        private void polling_Tick(object sender, EventArgs e)
        {
            //To-do: background colors for buttons
            //To-do: change play button to pause on hover and opposite 
            timeline.Visible = false;
            playButton.Visible = false;
            closeButton.Visible = false;
            pollingTimer.Stop();
        }

        private void control_MouseEnter(object sender, EventArgs e)
        {
            pollingTimer.Stop();
        }

        private void control_MouseLeave(object sender, EventArgs e)
        {
            pollingTimer.Start();
        }

        #endregion

        #region Progress bar

        static public ProgressBar CreateProgressBar(long savedTime, long length)
        {
            //To-do: min time before showing and max time before filling completely
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
                    //To-do: make sure right invoke https://stackoverflow.com/questions/229554/whats-the-difference-between-invoke-and-begininvoke
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
        }

        #endregion

        #region Media player 

        private void mediaPlayer_EndReached(object sender, EventArgs e)
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
                                    Console.WriteLine("going from season " + (i) + " to " + (i + 1));
                                    currSeason = currTvShow.Seasons[i + 1];
                                    currTvShow.CurrSeason++;
                                    currEpisode = currSeason.Episodes[0];
                                    path = currEpisode.Path;
                                    timeline.Value = 0;
                                    Media nextMedia = new Media(libVlc, path, FromType.FromPath);
                                    //To-do: log?
                                    Console.WriteLine("NEXT: " + path);
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
                                        mainForm.Invoke(new MethodInvoker(delegate { mainForm.UpdateTvForm(currTvShow); }));
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
                                Media nextMedia = new Media(libVlc, path, FromType.FromPath);
                                Console.WriteLine("NEXT: " + path);
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

        private void videoView1_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = PointToClient(Cursor.Position);
            if (p.Y < this.Height / 6)
            {
                pollingTimer.Enabled = true;
                pollingTimer.Start();
                playButton.Visible = true;
                closeButton.Visible = true;
                timeline.Visible = true;
            }
        }

        #endregion

        #region Timeline

        private void timeline_ValueChanged(object sender, long value)
        {
            if (mouseDown)
            {
                TimeSpan lengthTime = TimeSpan.FromMilliseconds(mediaPlayer.Length);
                TimeSpan seekTime = TimeSpan.FromMilliseconds(value);
                if (seekTime.TotalMilliseconds > lengthTime.TotalMilliseconds)
                {
                    timeline.Value = (long)lengthTime.TotalMilliseconds;
                }
                mediaPlayer.SeekTo(seekTime);
                string timeString;

                if (value > 3600000) //hour in ms
                { //To-do: show end time
                    timeString = seekTime.ToString(@"hh\:mm\:ss") + "/" + lengthTime.ToString(@"hh\:mm\:ss");
                }
                else
                {
                    timeString = seekTime.ToString(@"mm\:ss") + "/" + lengthTime.ToString(@"mm\:ss");
                }

                if (timePanelActive)
                {
                    try
                    {
#pragma warning disable CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception
                        timePanel.Location = new Point((int)(timeline._trackerRect.Location.X + timePanel.Width * 2.25), timeline.Location.Y - timePanel.Height - 10);
#pragma warning restore CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    timeLabel.Text = timeString;
                }
                else
                {
                    timePanel = new Panel();
                    try
                    {
#pragma warning disable CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception
                        timePanel.Location = new Point((int)(timeline._trackerRect.Location.X + timePanel.Width * 2.25), timeline.Location.Y - timePanel.Height - 10);
#pragma warning restore CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    Font f = new Font("Arial", 12, FontStyle.Bold);
                    timeLabel = new Label();
                    timeLabel.Text = timeString;
                    timeLabel.Font = f;
                    timeLabel.Padding = new Padding(2, 2, 0, 2);

                    timePanel.Controls.Add(timeLabel);
                    timePanel.ForeColor = SystemColors.Control;

                    this.Controls.Add(timePanel);
                    timePanel.BringToFront();

                    timePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    timePanel.AutoSize = true;
                    timeLabel.AutoSize = true;
                    timePanelActive = true;
                }
            }
        }

        private void timeline_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            timePanelActive = false;

            if (timePanel != null)
            {
                timePanel.Dispose();
            }

            TimeSpan ts = TimeSpan.FromMilliseconds((double)timeline.Value);
            mediaPlayer.SeekTo(ts);
        }

        private void timeline_MouseDown(object sender, MouseEventArgs e)
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
