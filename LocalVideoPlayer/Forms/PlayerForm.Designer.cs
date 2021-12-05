
namespace LocalVideoPlayer
{
    partial class PlayerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlayerForm));
            this.videoView1 = new LibVLCSharp.WinForms.VideoView();
            this.timeline = new XComponent.SliderBar.MACTrackBar();
            //this.timePanel1 = new System.Windows.Forms.Panel();
            //this.timeLabel1 = new System.Windows.Forms.Label();
            this.playButton = new LocalVideoPlayer.RoundButton();
            this.closeButton = new LocalVideoPlayer.RoundButton();
            ((System.ComponentModel.ISupportInitialize)(this.videoView1)).BeginInit();
            //this.timePanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // videoView1
            // 
            this.videoView1.BackColor = System.Drawing.Color.Black;
            this.videoView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoView1.Location = new System.Drawing.Point(0, 0);
            this.videoView1.MediaPlayer = null;
            this.videoView1.Name = "videoView1";
            this.videoView1.Size = new System.Drawing.Size(800, 450);
            this.videoView1.TabIndex = 4;
            this.videoView1.Text = "videoView1";
            this.videoView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.VideoView1_MouseMove);
            // 
            // timeline
            // 
            this.timeline.BackColor = System.Drawing.SystemColors.Desktop;
            this.timeline.BorderColor = System.Drawing.SystemColors.ActiveBorder;
            this.timeline.Cursor = System.Windows.Forms.Cursors.Hand;
            this.timeline.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeline.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(123)))), ((int)(((byte)(125)))), ((int)(((byte)(123)))));
            this.timeline.IndentHeight = 3;
            this.timeline.Location = new System.Drawing.Point(88, 392);
            this.timeline.Maximum = ((long)(1000));
            this.timeline.Minimum = ((long)(0));
            this.timeline.Name = "timeline";
            this.timeline.Size = new System.Drawing.Size(704, 38);
            this.timeline.TabIndex = 6;
            this.timeline.TextTickStyle = System.Windows.Forms.TickStyle.None;
            this.timeline.TickColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(146)))), ((int)(((byte)(148)))));
            this.timeline.TickHeight = 1;
            this.timeline.TickStyle = System.Windows.Forms.TickStyle.None;
            this.timeline.TrackerColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(130)))), ((int)(((byte)(198)))));
            this.timeline.TrackerSize = new System.Drawing.Size(32, 32);
            this.timeline.TrackLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(93)))), ((int)(((byte)(90)))));
            this.timeline.TrackLineHeight = 3;
            this.timeline.Value = ((long)(0));
            this.timeline.Visible = false;
            this.timeline.ValueChanged += new XComponent.SliderBar.ValueChangedHandler(this.Timeline_ValueChanged);
            this.timeline.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Timeline_MouseDown);
            this.timeline.MouseEnter += new System.EventHandler(this.Control_MouseEnter);
            this.timeline.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            this.timeline.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Timeline_MouseUp);
            // 
            // timePanel
            // 
            /*this.timePanel1.AutoSize = true;
            this.timePanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.timePanel1.Controls.Add(this.timeLabel1);
            this.timePanel1.ForeColor = System.Drawing.SystemColors.Control;
            this.timePanel1.Location = new System.Drawing.Point(432, 251);
            this.timePanel1.Name = "timePanel";
            this.timePanel1.Size = new System.Drawing.Size(113, 23);
            this.timePanel1.TabIndex = 11;
            this.timePanel1.Visible = false;
            // 
            // timeLabel
            // 
            this.timeLabel1.AutoSize = true;
            this.timeLabel1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeLabel1.Location = new System.Drawing.Point(3, 0);
            this.timeLabel1.Name = "timeLabel";
            this.timeLabel1.Padding = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.timeLabel1.Size = new System.Drawing.Size(107, 23);
            this.timeLabel1.TabIndex = 0;
            this.timeLabel1.Text = "00:00 / 00:00";*/
            // 
            // playButton
            // 
            this.playButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("playButton.BackgroundImage")));
            this.playButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.playButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.playButton.FlatAppearance.BorderSize = 0;
            this.playButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.playButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.playButton.Location = new System.Drawing.Point(8, 376);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(64, 64);
            this.playButton.TabIndex = 10;
            this.playButton.UseVisualStyleBackColor = true;
            this.playButton.Visible = false;
            this.playButton.Click += new System.EventHandler(this.PlayButton_Click);
            this.playButton.MouseEnter += new System.EventHandler(this.Control_MouseEnter);
            this.playButton.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            // 
            // closeButton
            // 
            this.closeButton.BackgroundImage = global::LocalVideoPlayer.Properties.Resources.close;
            this.closeButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.closeButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Location = new System.Drawing.Point(728, 8);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(64, 64);
            this.closeButton.TabIndex = 9;
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Visible = false;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            this.closeButton.MouseEnter += new System.EventHandler(this.Control_MouseEnter);
            this.closeButton.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            // 
            // PlayerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.ClientSize = new System.Drawing.Size(800, 450);
            //this.Controls.Add(this.timePanel1);
            this.Controls.Add(this.playButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.timeline);
            this.Controls.Add(this.videoView1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PlayerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.DeepPink;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PlayerForm_FormClosing);
            this.Load += new System.EventHandler(this.PlayerForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.videoView1)).EndInit();
            //this.timePanel1.ResumeLayout(false);
            //this.timePanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private LibVLCSharp.WinForms.VideoView videoView1;
        private XComponent.SliderBar.MACTrackBar timeline;
        private RoundButton closeButton;
        private RoundButton playButton;
        //private System.Windows.Forms.Panel timePanel1;
        //private System.Windows.Forms.Label timeLabel1;
    }
}