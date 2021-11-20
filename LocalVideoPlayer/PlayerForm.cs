using LocalVideoPlayer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalVideoPlayer
{
    public partial class PlayerForm : Form
    {
        private Vlc.DotNet.Forms.VlcControl vlcControl;
        private string path;
        private Timer pollingTimer;

        public PlayerForm(string p)
        {
            InitializeComponent();

            path = p;
            DirectoryInfo d = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            vlcControl = new Vlc.DotNet.Forms.VlcControl();
            vlcControl.BeginInit();
            vlcControl.VlcLibDirectory = d;
            vlcControl.VlcMediaplayerOptions = new[] { "" }; //"-vv"
            vlcControl.EndInit();
            vlcControl.Dock = DockStyle.Fill;

            //To-do: does mouse go away when idle
            vlcControl.Video.IsMouseInputEnabled = false;
            vlcControl.Video.IsKeyInputEnabled = false;
            vlcControl.MouseMove += vlcControl_MouseMove;
            this.Controls.Add(this.vlcControl);

            pollingTimer = new Timer();
            pollingTimer.Tick += new EventHandler(polling_Tick);
            pollingTimer.Interval = 2000;
        }

        private void polling_Tick(object sender, EventArgs e)
        {
            playButton.Visible = false;
            closeButton.Visible = false;
            pollingTimer.Stop();
            vlcControl.MouseMove += vlcControl_MouseMove;
        }

        private void PlayerForm_Load(object sender, EventArgs e)
        {
            closeButton.Location = new Point(this.Width - (int)(closeButton.Width * 1.5), (closeButton.Width / 2));
            playButton.Location = new Point(this.Width / 2, this.Height - (int)(playButton.Width * 1.5));
            FileInfo media = new FileInfo(path);
            vlcControl.Play(media);
        }

        private void vlcControl_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = PointToClient(Cursor.Position);
            if (p.Y < this.Height / 6)
            {
                pollingTimer.Enabled = true;
                pollingTimer.Start();
                playButton.Visible = true;
                closeButton.Visible = true;
                vlcControl.MouseMove -= vlcControl_MouseMove;
            }
        }

        private void PlayerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(pollingTimer != null)
            {
                if(pollingTimer.Enabled)
                {
                    pollingTimer.Stop();
                }
                pollingTimer.Dispose();
            }
            vlcControl.Dispose();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            if (vlcControl.IsPlaying)
            {
                playButton.BackgroundImage = Resources.pause;
                vlcControl.Pause();
                pollingTimer.Stop();
            }
            else
            {
                playButton.BackgroundImage = Resources.play;
                vlcControl.Play();
                pollingTimer.Start();
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
