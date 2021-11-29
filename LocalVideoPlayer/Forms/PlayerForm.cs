using LibVLCSharp.Shared;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    /*
        Do not call LibVLC from a LibVLC event without switching thread first
        Doing this:
        mediaPlayer.EndReached += (sender, args) => mediaPlayer.Play(nextMedia);
        Might freeze your app.
        If you need to call back into LibVLCSharp from an event, you need to switch thread. This is an example of how to do it:
        mediaPlayer.EndReached += (sender, args) => ThreadPool.QueueUserWorkItem(_ => mediaPlayer.Play(nextMedia);
    */

    public partial class PlayerForm : Form
    {
        public LibVLC libVlc;
        public MediaPlayer mediaPlayer;
        private TvShow tvShow;
        private Episode episode;
        private string path;
        private long seekTime;
        private int runningTime;
        private Timer pollingTimer;
        private bool mouseDown = false;
        private bool timePanelActive = false;
        private Panel timePanel = null;
        private Label timeLabel = null;

        public PlayerForm(string p, long s, int r, TvShow t, Episode ep)
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();

            episode = ep;
            tvShow = t;
            seekTime = s;
            //To-do: make the show/movie a parameter
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
                if(!mouseDown)
                {
                    timeline.Value = mediaPlayer.Time;  
                }

            };

            mediaPlayer.LengthChanged += (sender, e) =>
            {
                timeline.Maximum = mediaPlayer.Length;
            };

            mediaPlayer.EncounteredError += (sender, e) =>
            {
                throw new Exception("VLC error");
            };

            mediaPlayer.EndReached += (sender, e) =>
            {
                //To-do: go to next episode
                //To-do: move to next season
                //To-do: reset
                this.Invoke(new MethodInvoker(delegate { this.Dispose(); }));
                Console.WriteLine(path);
                Console.WriteLine("HERE");

                /*if(playNext)
                       {
                           Season currentSeason = currTvShow.Seasons[currSeason - 1];

                           for (int i = 0; i < currentSeason.Episodes.Length; i++)
                           {
                               if (currEpisode == currentSeason.Episodes[i])
                               {
                                   if (i == currentSeason.Episodes.Length - 1)
                                   {
                                       throw new NotImplementedException();
                                       //go to next season

                                       //chec last season -> reset? (skip extraas...)
                                   }
                                   else
                                   {
                                       Episode nextEpisode = currentSeason.Episodes[i + 1];
                                       LaunchVlc(currTvShow.Name, nextEpisode.Name, nextEpisode.Path);
                                   }
                               }
                           }
                       }*/
            };

            /*mediaPlayer.Buffering += (sender, e) =>
            {
                
            };*/

            pollingTimer = new Timer();
            pollingTimer.Tick += new EventHandler(polling_Tick);
            pollingTimer.Interval = 2000;
        }

        private void polling_Tick(object sender, EventArgs e)
        {
            //To-do: background colors for buttons
            //To-do: change play button to pause on hover and opposite 
            timeline.Visible = false;
            timeline.Visible = false;
            playButton.Visible = false;
            closeButton.Visible = false;
            pollingTimer.Stop();
        }

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            closeButton.Location = new Point(this.Width - (int)(closeButton.Width * 1.25), (closeButton.Width / 4));
            playButton.Location = new Point(playButton.Width / 4, this.Height - (int)(playButton.Width * 1.25));
            timeline.Size = new Size(this.Width - (int)(playButton.Width * 4), playButton.Height / 2);
            timeline.Location = new Point(playButton.Width * 2, this.Height - playButton.Height);

            this.Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(0, this.Height * 2);

            FileInfo media = new FileInfo(path);
            Media currentMedia = new Media(libVlc, path, FromType.FromPath);
            bool result = mediaPlayer.Play(currentMedia);

            if (seekTime != 0 && result)
            {
                if(seekTime < runningTime * 60000)
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

            //To-do: all bars full = a reset
            this.Text = mediaPlayer.Time.ToString();

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

        private void timeline_ValueChanged(object sender, long value)
        {
            if (mouseDown)
            {
                TimeSpan seekTime = TimeSpan.FromMilliseconds(value);
                mediaPlayer.SeekTo(seekTime);
                string timeString;

                if(value > 3600000) //hour in ms
                {
                    timeString = seekTime.ToString(@"hh\:mm\:ss");
                } else
                {
                    timeString = seekTime.ToString(@"mm\:ss");
                }

                if (timePanelActive)
                {
                    try
                    {
#pragma warning disable CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception
                        timePanel.Location = new Point((int)(timeline._trackerRect.Location.X + timePanel.Width * 2.25), timeline.Location.Y - timePanel.Height - 10);
#pragma warning restore CS1690 // Accessing a member on a field of a marshal-by-reference class may cause a runtime exception
                    }
                    catch(Exception e) 
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
            
            if(timePanel != null)
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

        private void control_MouseEnter(object sender, EventArgs e)
        {
            pollingTimer.Stop();
        }

        private void control_MouseLeave(object sender, EventArgs e)
        {
            pollingTimer.Start();
        }
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
