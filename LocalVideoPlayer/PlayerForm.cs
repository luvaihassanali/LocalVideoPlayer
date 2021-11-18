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
            FileInfo media = new FileInfo(path);
            vlcControl.Play(media);
            Console.WriteLine(this.Width + " " + this.Height);
            closeButton.Location = new Point(this.Width - closeButton.Width * 2, closeButton.Width);
            playButton.Location = new Point(this.Width / 2, this.Height - 100);
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

        private void playButton_MouseClick(object sender, MouseEventArgs e)
        {
            if(vlcControl.IsPlaying)
            {
                playButton.Text = "||";
                vlcControl.Pause();
            } else
            {
                playButton.Text = ">";
                vlcControl.Play();
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

        private void closeButton_MouseClick(object sender, MouseEventArgs e)
        {
            this.Close();
        }
    }
}
