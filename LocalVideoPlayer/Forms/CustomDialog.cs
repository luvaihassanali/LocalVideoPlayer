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

        #region Reset seasons

        static internal int[] ShowResetSeasons(TvShow tvShow, int width, int height)
        {
            Font headerFont = new Font("Arial", 16, FontStyle.Bold);
            Font textFont = new Font("Arial", 12, FontStyle.Regular);
            Form resetForm = new Form();
            CustomScrollbar customScrollbar = null;
            List<Control> controls = new List<Control>();
            List<int> selectionList = new List<int>();
            bool fill = false;

            resetForm.Width = (int)(width / 3);
            resetForm.Height = (int)(height / 1.5);
            resetForm.MaximumSize = new Size(width, height - 100);
            resetForm.StartPosition = FormStartPosition.CenterScreen;
            resetForm.BackColor = Color.Black;
            resetForm.ForeColor = SystemColors.Control;
            resetForm.ShowInTaskbar = false;
            resetForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            resetForm.ControlBox = false;

            Panel resetFormMainPanel = new Panel();
            resetFormMainPanel.Size = resetForm.Size;
            resetFormMainPanel.MaximumSize = resetForm.MaximumSize;
            resetFormMainPanel.AutoScroll = true;
            resetFormMainPanel.Name = "resetFormMainPanel";
            resetForm.Controls.Add(resetFormMainPanel);

            resetForm.Shown += (s, e) =>
            {
                resetFormMainPanel.AutoScrollPosition = new Point(0, 0);
            };

            resetForm.Deactivate += (s, e) =>
            {
                resetForm.Activate();
                return;
            };
            String epString = tvShow.LastEpisode == null ? "-" : tvShow.LastEpisode.Id.ToString();
            Label headerLabel = new Label() { Text = "Reset " + tvShow.Name + " (" + "S" + tvShow.CurrSeason + "E" + epString + ")" };
            headerLabel.Dock = DockStyle.Top;
            headerLabel.Font = headerFont;
            headerLabel.AutoSize = true;
            headerLabel.Padding = new Padding(20, 20, 20, 15);
            Size maxSize = new Size(width / 3, height);
            headerLabel.MaximumSize = maxSize;

            Button confirmClear = new Button() { Text = "Clear" };
            confirmClear.AutoSize = true;
            confirmClear.Font = textFont;
            confirmClear.Dock = DockStyle.Bottom;
            confirmClear.FlatStyle = FlatStyle.Flat;
            confirmClear.Cursor = blueHandCursor;
            confirmClear.Click += (sender, e) =>
            {
                fill = false;
                bool selection = false;
                foreach (Control c in controls)
                {
                    CheckBox box = c as CheckBox;
                    if (box != null)
                    {
                        if (box.Checked)
                        {
                            selection = true;
                            selectionList.Add(int.Parse(box.Name));
                        }
                    }
                }

                if (selection)
                {
                    resetForm.Close();
                }
            };

            Button confirmFill = new Button() { Text = "Fill" };
            confirmFill.AutoSize = true;
            confirmFill.Font = textFont;
            confirmFill.Dock = DockStyle.Bottom;
            confirmFill.FlatStyle = FlatStyle.Flat;
            confirmFill.Cursor = blueHandCursor;
            confirmFill.Click += (sender, e) =>
            {
                fill = true;
                bool selection = false;
                foreach (Control c in controls)
                {
                    CheckBox box = c as CheckBox;
                    if (box != null)
                    {
                        if (box.Checked)
                        {
                            selection = true;
                            selectionList.Add(int.Parse(box.Name));
                        }
                    }
                }

                if (selection)
                {
                    if(!(selectionList[0] == 0))
                    {
                        resetForm.Close();
                    }
                }
            };

            Button cancel = new Button() { Text = "Cancel" };
            cancel.AutoSize = true;
            cancel.Font = textFont;
            cancel.Dock = DockStyle.Bottom;
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.Cursor = blueHandCursor;
            cancel.Click += (sender, e) => { resetForm.Close(); };

            int numSeasons = tvShow.Seasons.Length;
            for (int i = 0; i <= numSeasons; i++)
            {
                CheckBox chkBox = new CheckBox { Text = "Season " + i };
                chkBox.Dock = DockStyle.Top;
                chkBox.Font = textFont;
                chkBox.AutoSize = true;
                chkBox.Cursor = blueHandCursor;
                chkBox.Padding = new Padding(40, 20, 20, 0);
                chkBox.Name = i.ToString();
                if (i == 0)
                {
                    chkBox.Text = "All seasons";
                    chkBox.Click += (sender, e) =>
                    {
                        resetFormMainPanel.AutoScrollPosition = new Point(confirmClear.Location.X, confirmClear.Location.Y);
                        if (customScrollbar != null)
                        {
                            customScrollbar.Value = -resetFormMainPanel.AutoScrollPosition.Y;
                        }
                    };
                }

                if (i == numSeasons)
                {
                    chkBox.Padding = new Padding(40, 20, 20, 40);
                }

                controls.Add(chkBox);
            }

            resetFormMainPanel.Controls.Add(headerLabel);
            resetFormMainPanel.Controls.Add(confirmClear);
            resetFormMainPanel.Controls.Add(confirmFill);
            resetFormMainPanel.Controls.Add(cancel);
            controls.Reverse();
            foreach (Control c in controls)
            {
                resetFormMainPanel.Controls.Add(c);
            }
            resetFormMainPanel.Controls.Add(headerLabel);

            if (resetFormMainPanel.VerticalScroll.Visible)
            {
                customScrollbar = CreateScrollBar(resetFormMainPanel);
                resetForm.Width += 2;
                resetFormMainPanel.Padding = new Padding(0, 0, customScrollbar.Width / 2 - 2, 2);
                customScrollbar.Scroll += (s, e) =>
                {
                    resetFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
                };

                resetFormMainPanel.MouseWheel += (s, e) =>
                {
                    int newVal = -resetFormMainPanel.AutoScrollPosition.Y;
                    customScrollbar.Value = newVal;
                };

                resetForm.Controls.Add(customScrollbar);
                customScrollbar.BringToFront();
            }

            resetForm.ShowDialog();
            resetForm.Dispose();

            if(fill)
            {
                selectionList.Insert(0, 1);
            } else
            {
                selectionList.Insert(0, 0);
            }
            return selectionList.ToArray();         
        }

        #endregion

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
            optionsForm.BackColor = Color.Black;
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

            Button confirm = new Button() { Text = "OK" };
            confirm.AutoSize = true;
            confirm.Font = textFont;
            confirm.Dock = DockStyle.Bottom;
            confirm.FlatStyle = FlatStyle.Flat;
            confirm.Cursor = blueHandCursor;

            confirm.Click += (sender, e) =>
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
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirm.Location.X, confirm.Location.Y);
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
                    optionsFormMainPanel.AutoScrollPosition = new Point(confirm.Location.X, confirm.Location.Y);
                    if (customScrollbar != null)
                    {
                        customScrollbar.Value = -optionsFormMainPanel.AutoScrollPosition.Y;
                    }
                };
                controls.Add(descLabel);
            }

            optionsFormMainPanel.Controls.Add(headerLabel);
            optionsFormMainPanel.Controls.Add(confirm);

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
                optionsFormMainPanel.Padding = new Padding(0, 0, customScrollbar.Width / 2 - 2, 2);
                customScrollbar.Scroll += (s, e) =>
                {
                    optionsFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
                };

                optionsFormMainPanel.MouseWheel += (s, e) =>
                {
                    int newVal = -optionsFormMainPanel.AutoScrollPosition.Y;
                    customScrollbar.Value = newVal;
                };

                optionsForm.Controls.Add(customScrollbar);
                customScrollbar.BringToFront();
            }

            optionsForm.BringToFront();
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
            customMessageForm.BackColor = Color.Black;
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

            Button confirm = new Button() { Text = "OK" };
            confirm.AutoSize = true;
            confirm.Font = textFont;
            confirm.Dock = DockStyle.Bottom;
            confirm.FlatStyle = FlatStyle.Flat;
            confirm.Cursor = blueHandCursor;
            confirm.Click += (sender, e) => { customMessageForm.Close(); };
            customMessageForm.Controls.Add(confirm);

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
            customScrollbar.ChannelColor = SystemColors.ControlDark; //Color.FromArgb(((int)(((byte)(53)))), ((int)(((byte)(193)))), ((int)(((byte)(241))))); //green 51 166 3 //blue 22 88 14
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
            customScrollbar.LargeChange = (int)(customScrollbar.Maximum / customScrollbar.Height + panel.Height / 1.25);
            customScrollbar.Value = Math.Abs(panel.AutoScrollPosition.Y);
            customScrollbar.Cursor = blueHandCursor;

            if (panel.Name.Equals("mainFormMainPanel"))
            {
                customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + (int)(panel.Height / 1.5);
                customScrollbar.Location = new Point(panel.Width - 32, 0);
            } 
            else if (panel.Name.Equals("tvFormMainPanel"))
            {
                panel.Width -= 13;
            }

            return customScrollbar;
        }

        static internal void UpdateScrollBar(CustomScrollbar customScrollbar, Panel panel)
        {
            customScrollbar.Size = new Size(30, panel.Height);
            customScrollbar.LargeChange = customScrollbar.Maximum / customScrollbar.Height + panel.Height;
            customScrollbar.Maximum = panel.DisplayRectangle.Height;
        }
    }
}
