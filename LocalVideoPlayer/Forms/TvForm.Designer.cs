
namespace LocalVideoPlayer.Forms
{
    partial class TvForm
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
            this.seasonButton = new System.Windows.Forms.Button();
            this.resumeButton = new System.Windows.Forms.Button();
            this.closeButton = new LocalVideoPlayer.RoundButton();
            this.SuspendLayout();
            // 
            // seasonButton
            // 
            this.seasonButton.AutoSize = true;
            this.seasonButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.seasonButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.seasonButton.Location = new System.Drawing.Point(168, 328);
            this.seasonButton.Name = "seasonButton";
            this.seasonButton.Size = new System.Drawing.Size(75, 23);
            this.seasonButton.TabIndex = 11;
            this.seasonButton.UseVisualStyleBackColor = true;
            this.seasonButton.Visible = false;
            // 
            // resumeButton
            // 
            this.resumeButton.AutoSize = true;
            this.resumeButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.resumeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.resumeButton.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resumeButton.Location = new System.Drawing.Point(182, 172);
            this.resumeButton.Name = "resumeButton";
            this.resumeButton.Size = new System.Drawing.Size(84, 31);
            this.resumeButton.TabIndex = 12;
            this.resumeButton.Text = "Resume";
            this.resumeButton.UseVisualStyleBackColor = true;
            this.resumeButton.Visible = false;
            // 
            // closeButton
            // 
            this.closeButton.BackgroundImage = global::LocalVideoPlayer.Properties.Resources.close;
            this.closeButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Location = new System.Drawing.Point(376, 0);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(64, 64);
            this.closeButton.TabIndex = 10;
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // TvForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.ClientSize = new System.Drawing.Size(438, 367);
            this.Controls.Add(this.resumeButton);
            this.Controls.Add(this.seasonButton);
            this.Controls.Add(this.closeButton);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "TvForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "tvForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private RoundButton closeButton;
        private System.Windows.Forms.Button seasonButton;
        private System.Windows.Forms.Button resumeButton;
    }
}