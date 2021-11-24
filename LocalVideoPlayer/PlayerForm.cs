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
        private Timer pollingTimer;

        public PlayerForm(string p, long s)
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            this.DoubleBuffered = true;

            seekTime = s;
            path = p;
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
            
            playButton.Visible = false;
            closeButton.Visible = false;
            pollingTimer.Stop();
        }

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            closeButton.Location = new Point(this.Width - (int)(closeButton.Width * 1.5), (closeButton.Width / 2));
            playButton.Location = new Point(this.Width / 2, this.Height - (int)(playButton.Width * 1.5));
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
            }
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
