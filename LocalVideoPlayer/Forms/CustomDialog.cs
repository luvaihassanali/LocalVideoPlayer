using CustomControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LocalVideoPlayer.Forms
{
    static class CustomDialog
    {
        static private Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);

        #region Show Options Form

        static internal int ShowOptions(string item, string[][] info, DateTime?[] dates, int width, int height)
        {
            Font headerFont = new Font("Arial", 16, FontStyle.Bold);
            Font textFont = new Font("Arial", 12, FontStyle.Regular);
            Form optionsForm = new Form();
            CustomScrollbar customScrollbar = null;
            List<Control> controls = new List<Control>();

            optionsForm.Width = (int)(width / 1.5);
            optionsForm.Height = (int)(height / 1.5);
            optionsForm.MaximumSize = new Size(width, height - 100);
            optionsForm.StartPosition = FormStartPosition.CenterScreen;
            optionsForm.BackColor = SystemColors.Desktop;
            optionsForm.ForeColor = SystemColors.Control;
            optionsForm.ShowInTaskbar = false;
            optionsForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            optionsForm.ControlBox = false;

            Panel optionsFormMainPanel = new Panel();
            optionsFormMainPanel.Size = optionsForm.Size;
            optionsFormMainPanel.MaximumSize = optionsForm.MaximumSize;
            optionsFormMainPanel.AutoScroll = true;
            optionsFormMainPanel.Name = "optionsFormMainPanel";
            optionsForm.Controls.Add(optionsFormMainPanel);

            optionsForm.Shown += (s, e) =>
            {
                optionsFormMainPanel.AutoScrollPosition = new Point(0, 0);
            };

            optionsForm.Deactivate += (s, e) =>
            {
                optionsForm.Activate();
                return;
            };

            Label headerLabel = new Label() { Text = item + "?" };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = headerFont;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 15);
            Size maxSize = new Size(width / 2, height);
            headerLabel.MaximumSize = maxSize;

            Button confirmation = new Button() { Text = "OK" };
            confirmation.AutoSize = true;
            confirmation.Font = textFont;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = blueHandCursor;

            confirmation.Click += (sender, e) =>
            {
                bool selection = false;
                foreach (Control c in controls)
                {
                    RadioButton btn = c as RadioButton;
                    if (btn != null)
                    {
                        if (btn.Checked)
                        {
                            selection = true;
                        }
                    }
                }

                if (selection)
                {
                    optionsForm.Close();
                }
            };

            for (int i = 0; i < info[0].Length; i++)
            {
                RadioButton radioBtn = new RadioButton { Text = info[0][i] + " (" + dates[i].GetValueOrDefault().Year + ")" };
                radioBtn.Dock = DockStyle.Top;
                radioBtn.Font = textFont;
                radioBtn.AutoSize = true;
                radioBtn.Cursor = blueHandCursor;
                radioBtn.Padding = new Padding(20, 20, 20, 0);
                radioBtn.Name = info[1][i];
                radioBtn.Click += (sender, e) =>
                {
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirmation.Location.X, confirmation.Location.Y);
                    if (customScrollbar != null)
                    {
                        customScrollbar.Value = -optionsFormMainPanel.AutoScrollPosition.Y;
                    }
                };
                controls.Add(radioBtn);

                Label descLabel = new Label() { Text = info[2][i].Equals(String.Empty) ? "No description." : info[2][i] };
                descLabel.Dock = DockStyle.Top;
                descLabel.Font = textFont;
                descLabel.AutoSize = true;
                descLabel.Padding = new Padding(20);
                Size s = new Size(optionsForm.Width - (descLabel.Width / 2), optionsForm.Height);
                descLabel.MaximumSize = s;
                descLabel.Cursor = blueHandCursor;
                descLabel.Click += (sender, e) =>
                {
                    radioBtn.Checked = true;
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirmation.Location.X, confirmation.Location.Y);
                    if (customScrollbar != null)
                    {
                        customScrollbar.Value = -optionsFormMainPanel.AutoScrollPosition.Y;
                    }
                };
                controls.Add(descLabel);
            }

            optionsFormMainPanel.Controls.Add(headerLabel);
            optionsFormMainPanel.Controls.Add(confirmation);

            controls.Reverse();
            foreach (Control c in controls)
            {
                optionsFormMainPanel.Controls.Add(c);
            }
            optionsFormMainPanel.Controls.Add(headerLabel);

            if (optionsFormMainPanel.VerticalScroll.Visible)
            {
                customScrollbar = CreateScrollBar(optionsFormMainPanel);
                optionsForm.Width += 2;

                customScrollbar.Scroll += (s, e) =>
                {
                    optionsFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
                };

                optionsFormMainPanel.MouseWheel += (s, e) =>
                {
                    int newVal = -optionsFormMainPanel.AutoScrollPosition.Y;
                    customScrollbar.Value = newVal;
                    customScrollbar.Invalidate();
                    Application.DoEvents();
                };

                optionsForm.Controls.Add(customScrollbar);
                customScrollbar.BringToFront();
            }

            optionsForm.ShowDialog();
            optionsForm.Dispose();

            int id = 0;
            foreach (Control c in controls)
            {
                RadioButton btn = c as RadioButton;
                if (btn != null)
                {
                    if (btn.Checked)
                    {
                        id = Int32.Parse(btn.Name);
                    }
                }
            }

            if (id == 0) throw new ArgumentNullException();

            return id;
        }

        #endregion

        #region Show Message Form

        static internal void ShowMessage(string header, string message, int width, int height)
        {
            Font headerFont = new Font("Arial", 16, FontStyle.Bold);
            Font textFont = new Font("Arial", 12, FontStyle.Regular);
            Form customMessageForm = new Form();

            customMessageForm.Width = width / 2;
            customMessageForm.Height = height / 6;
            customMessageForm.MaximumSize = new Size(width, height);
            customMessageForm.ShowInTaskbar = false;
            customMessageForm.AutoScroll = true;
            customMessageForm.AutoSize = true;
            customMessageForm.StartPosition = FormStartPosition.CenterScreen;
            customMessageForm.BackColor = SystemColors.Desktop;
            customMessageForm.ForeColor = SystemColors.Control;
            customMessageForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            customMessageForm.ControlBox = false;

            Label textLabel = new Label() { Text = message };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = textFont;
            textLabel.AutoSize = true;
            textLabel.Padding = new Padding(20, 20, 20, 20);
            Size maxSize = new Size(width / 2, height);
            textLabel.MaximumSize = maxSize;
            customMessageForm.Controls.Add(textLabel);

            Label headerLabel = new Label() { Text = header };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = headerFont;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 15);
            customMessageForm.Controls.Add(headerLabel);

            Button confirmation = new Button() { Text = "OK" };
            confirmation.AutoSize = true;
            confirmation.Font = textFont;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = blueHandCursor;
            confirmation.Click += (sender, e) => { customMessageForm.Close(); };
            customMessageForm.Controls.Add(confirmation);

            customMessageForm.Deactivate += (s, e) =>
            {
                customMessageForm.Activate();
                return;
            };

            customMessageForm.ShowDialog();
            customMessageForm.Dispose();
        }

        #endregion

        static internal CustomScrollbar CreateScrollBar(Panel panel)
        {
            CustomScrollbar customScrollbar = new CustomScrollbar();
            customScrollbar.ChannelColor = Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(88)))), ((int)(((byte)(140))))); //green 51 166 3 //blue 22 88 14
            customScrollbar.Location = new Point(panel.Width - 30, 0);
            customScrollbar.Size = new Size(30, panel.Height);
            customScrollbar.DownArrowImage = Properties.Resources.downarrow;
            customScrollbar.ThumbBottomImage = Properties.Resources.ThumbBottom;
            customScrollbar.ThumbBottomSpanImage = Properties.Resources.ThumbSpanBottom;
            customScrollbar.ThumbMiddleImage = Properties.Resources.ThumbMiddle;
            customScrollbar.ThumbTopImage = Properties.Resources.ThumbTop;
            customScrollbar.ThumbTopSpanImage = Properties.Resources.ThumbSpanTop;
            customScrollbar.UpArrowImage = Properties.Resources.uparrow;
            customScrollbar.Minimum = 0;
            customScrollbar.Maximum = panel.DisplayRectangle.Height;
            customScrollbar.Name = "customScrollbar";
            customScrollbar.SmallChange = 15;
            customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + panel.Height;
            customScrollbar.Value = Math.Abs(panel.AutoScrollPosition.Y);
            customScrollbar.Cursor = blueHandCursor;

            if (panel.Name.Equals("mainFormMainPanel"))
            {
                customScrollbar.Size = new Size(30, panel.Height - 1);
                customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + (int)(panel.Height / 1.068);
                customScrollbar.Location = new Point(panel.Width - 32, 0);
            }

            return customScrollbar;
        }

        static internal void UpdateScrollBar(CustomScrollbar customScrollbar, Panel panel)
        {
            customScrollbar.Size = new Size(15, panel.Height);
            customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + panel.Height;
            customScrollbar.Maximum = panel.DisplayRectangle.Height;
        }
    }
}
