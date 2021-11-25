using LibVLCSharp.Shared;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    public partial class PlayerForm : Form
    {
        public LibVLC libVlc;
        public MediaPlayer mediaPlayer;

        private string path;
        private long seekTime;
        private int runningTime;
        private Timer pollingTimer;
        private bool mouseDown = false;
        private bool screenshotActive = false;
        private Panel screenshot = null;
        public PlayerForm(string p, long s, int r)
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            this.DoubleBuffered = true;

            seekTime = s;
            path = p;
            runningTime = r;

            long max = runningTime * 60000;
            timeline.Maximum = max;
            timeline.Value = seekTime;

            DirectoryInfo d = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

            libVlc = new LibVLC();
            mediaPlayer = new MediaPlayer(libVlc);
            //To-do: move off screen when form launches
            mediaPlayer.EnableMouseInput = false;
            mediaPlayer.EnableKeyInput = false;
            //To-do: forward skip buttons
            //To-do: netflix type skipper
            //To-do: button pop up +0
            videoView1.MediaPlayer = mediaPlayer;

            mediaPlayer.TimeChanged += (sender, e) =>
            {
                if(!mouseDown)
                {
                    timeline.Value = mediaPlayer.Time;  
                }

            };

            mediaPlayer.EncounteredError += (sender, e) =>
            {
                Console.WriteLine("An error occurred");
            };

            mediaPlayer.EndReached += (sender, e) =>
            {
                Console.WriteLine("End reached");
            };

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

            FileInfo media = new FileInfo(path);
            bool result = mediaPlayer.Play(new Media(libVlc, path, FromType.FromPath));
            if(seekTime != 0 && result)
            {
                mediaPlayer.SeekTo(TimeSpan.FromMilliseconds(seekTime));
            }
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
                playButton.BackgroundImage = Properties.Resources.pause;
                mediaPlayer.Pause();
                pollingTimer.Stop();
            }
            else
            {
                playButton.BackgroundImage = Properties.Resources.play;
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
                timeline.Visible = true;
            }
        }

        private void timeline_ValueChanged(object sender, decimal value)
        {
            if (mouseDown)
            {
                if (screenshotActive)
                {
                    screenshot.Location = new Point((int)(timeline._trackerRect.Location.X + timeline._trackerRect.Width / 2), (int)(timeline._trackerRect.Location.Y - timeline._trackerRect.Height));
                }
                else
                {
                    screenshot = new Panel();
                    int width = playButton.Width * 4;
                    screenshot.Size = new Size(width, (int)(width / 1.777777777777778));
                    screenshot.Location = new Point((int)(timeline._trackerRect.Location.X + timeline._trackerRect.Width / 2), (int)(timeline._trackerRect.Location.Y - timeline._trackerRect.Height));
                    screenshot.BackColor = Color.Red;
                    this.Controls.Add(screenshot);
                    screenshot.BringToFront();
                    screenshotActive = true;
                }

            }
        }

        private void timeline_MouseUp(object sender, MouseEventArgs e)
        {
            pollingTimer.Start();
            mouseDown = false;
            screenshot.Dispose();
            screenshotActive = false;
            TimeSpan ts = TimeSpan.FromMilliseconds((double)timeline.Value);
            mediaPlayer.SeekTo(ts);
        }

        private void timeline_MouseDown(object sender, MouseEventArgs e)
        {
            pollingTimer.Stop();
            mouseDown = true;
        }
    }

    public class RoundButton : Button
    {
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            System.Drawing.Drawing2D.GraphicsPath graphicsPath = new System.Drawing.Drawing2D.GraphicsPath();
            graphicsPath.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new System.Drawing.Region(graphicsPath);
            base.OnPaint(e);
        }
    }
}
