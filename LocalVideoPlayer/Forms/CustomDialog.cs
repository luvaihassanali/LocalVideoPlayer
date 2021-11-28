using CustomControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LocalVideoPlayer.Forms
{
    static class CustomDialog
    {
        static internal int ShowOptions(string item, string[][] info, DateTime?[] dates, int width, int height)
        {
            CustomScrollbar customScrollbar = null;
            Form optionsForm = new Form();
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

            Font headerFont = new Font("Arial", 16, FontStyle.Bold);
            Font textFont = new Font("Arial", 12, FontStyle.Regular);

            Label headerLabel = new Label() { Text = "Choose a selection for: " + item };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = headerFont;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 15);
            Size maxSize = new Size(width / 2, height);
            headerLabel.MaximumSize = maxSize;

            List<Control> controls = new List<Control>();

            Button confirmation = new Button() { Text = "OK" };
            confirmation.AutoSize = true;
            confirmation.Font = textFont;
            confirmation.Dock = DockStyle.Bottom;
            confirmation.FlatStyle = FlatStyle.Flat;
            confirmation.Cursor = Cursors.Hand;
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
                RadioButton r1 = new RadioButton { Text = info[0][i] + " (" + dates[i].GetValueOrDefault().Year + ")" };
                r1.Dock = DockStyle.Top;
                r1.Font = textFont;
                r1.AutoSize = true;
                r1.Cursor = Cursors.Hand;
                r1.Padding = new Padding(20, 20, 20, 0);
                r1.Name = info[1][i];
                r1.Click += (sender, e) =>
                {
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirmation.Location.X, confirmation.Location.Y);
                    if(customScrollbar != null)
                    {
                        customScrollbar.Value = -optionsFormMainPanel.AutoScrollPosition.Y;
                    }
                };
                controls.Add(r1);

                Label l1 = new Label() { Text = info[2][i].Equals(String.Empty) ? "No description." : info[2][i] };
                l1.Dock = DockStyle.Top;
                l1.Font = textFont;
                l1.AutoSize = true;
                l1.Padding = new Padding(20);
                Size s = new Size(optionsForm.Width - (l1.Width / 2), optionsForm.Height);
                l1.MaximumSize = s;
                l1.Cursor = Cursors.Hand;
                l1.Click += (sender, e) =>
                {
                    r1.Checked = true;
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirmation.Location.X, confirmation.Location.Y);
                    if (customScrollbar != null)
                    {
                        customScrollbar.Value = -optionsFormMainPanel.AutoScrollPosition.Y;
                    }
                };
                controls.Add(l1);
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

        static internal void ShowMessage(string header, string message, int width, int height)
        {
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

            Font headerFont = new Font("Arial", 16, FontStyle.Bold);
            Font textFont = new Font("Arial", 12, FontStyle.Regular);

            Label textLabel = new Label() { Text = message };
            textLabel.Dock = DockStyle.Top;
            textLabel.Font = textFont;
            textLabel.AutoSize = true;
            textLabel.Padding = new Padding(20);
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
            confirmation.Cursor = Cursors.Hand;
            confirmation.Click += (sender, e) => { customMessageForm.Close(); };
            customMessageForm.Controls.Add(confirmation);

            customMessageForm.ShowDialog();
            customMessageForm.Dispose();
        }

        static internal CustomScrollbar CreateScrollBar(Panel panel)
        {
            CustomScrollbar customScrollbar = new CustomScrollbar();
            customScrollbar.ChannelColor = Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(166)))), ((int)(((byte)(3)))));
            customScrollbar.Location = new Point(panel.Width - 16, 0);
            customScrollbar.Size = new Size(15, panel.Height - 2);
            customScrollbar.DownArrowImage = Properties.Resources.downarrow;
            customScrollbar.ThumbBottomImage = Properties.Resources.ThumbBottom;
            customScrollbar.ThumbBottomSpanImage = Properties.Resources.ThumbSpanBottom;
            customScrollbar.ThumbMiddleImage = Properties.Resources.ThumbMiddle;
            customScrollbar.ThumbTopImage = Properties.Resources.ThumbTop;
            customScrollbar.ThumbTopSpanImage = Properties.Resources.ThumbSpanTop;
            customScrollbar.UpArrowImage = Properties.Resources.uparrow;
            customScrollbar.Minimum = 0;
            customScrollbar.Maximum = panel.DisplayRectangle.Height;
            customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + (int)(panel.Height / 1) + 3;
            customScrollbar.SmallChange = 1;
            customScrollbar.Value = Math.Abs(panel.AutoScrollPosition.Y);
            return customScrollbar;
        }

    }
}
