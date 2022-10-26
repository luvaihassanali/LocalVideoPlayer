using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomControls;
using LocalVideoPlayer.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace LocalVideoPlayer
{
    public partial class MainForm : Form
    {
        #region Dll import

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);
        private const int SPI_SETCURSORS = 0x0057;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;
        [DllImport("user32.dll")]

        static public extern bool ShowScrollBar(System.IntPtr hWnd, int wBar, bool bShow);

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO Dummy);
        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();

        public static uint GetIdleTime()
        {
            LASTINPUTINFO LastUserAction = new LASTINPUTINFO();
            LastUserAction.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(LastUserAction);
            GetLastInputInfo(ref LastUserAction);
            return ((uint)Environment.TickCount - LastUserAction.dwTime);
        }

        public static long GetTickCount()
        {
            return Environment.TickCount;
        }

        public static long GetLastInputTime()
        {
            LASTINPUTINFO LastUserAction = new LASTINPUTINFO();
            LastUserAction.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(LastUserAction);
            if (!GetLastInputInfo(ref LastUserAction))
            {
                throw new Exception(GetLastError().ToString());
            }

            return LastUserAction.dwTime;
        }

        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        #endregion

        public const string jsonFile = "Media.json";
        static public Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);
        static public Form dimmerForm;
        static public Form seasonDimmerForm;
        static public LayoutController layoutController;
        static public MediaModel media;
        static public Point mainFormLoc;
        static public Size mainFormSize;
        static public bool cartoonShuffle = false;
        static public int cartoonIndex = 0;
        static public int cartoonLimit = 20;
        static public List<TvShow> cartoons = new List<TvShow>();
        static public List<Episode> cartoonShuffleList = new List<Episode>();
        static private bool debugLogEnabled;
        static private string debugLogPath;
        static public bool hideCursor = bool.Parse(ConfigurationManager.AppSettings["hideCursor"]);
        static public int cursorCount = 0;
        private bool mouseMoverClientKill = false;
        private CustomScrollbar customScrollbar = null;
        private Label movieLabel;
        private Label cartoonsLabel;
        private Label tvLabel;
        public MouseWorker worker = null;
        private Panel mainFormMainPanel = null;
        private System.Threading.Timer idleMainFormTimer = null;

        public MainForm()
        {
            InitializeMainFormComponents();
            InitializeCustomCursor();
            InitializeDimmers();
            InitializeMouseWorker();
            InitializeIdleTimer();
#if DEBUG
            this.WindowState = FormWindowState.Normal;
#endif

            closeButton.Click += new EventHandler(CloseButton_Click);

            backgroundWorker1.DoWork += new DoWorkEventHandler(BackgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.RunWorkerAsync();
        }

        #region General form functions

        private void MainForm_Load(object sender, EventArgs e)
        {
            loadingCircle1.Location = new Point(this.Width / 2 - loadingCircle1.Width / 2, this.Height / 2 - loadingCircle1.Height / 2);
            loadingLabel.Location = new Point(0, this.Height / 2 - loadingLabel.Height / 2 + 2);
            loadingLabel.Size = new Size(this.Width, loadingLabel.Height);
            Log("Application start");
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            mainFormSize = this.Size;
            mainFormLoc = this.Location;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool getLastEpisodeList = true;
            if (getLastEpisodeList)
            {
                for (int i = 0; i < media.TvShows.Length; i++)
                {
                    string lastEpisodeString = media.TvShows[i].LastEpisode == null ? "null" : media.TvShows[i].LastEpisode.Name + " (Season " + media.TvShows[i].CurrSeason + ")" + Environment.NewLine + media.TvShows[i].LastEpisode.Path;
                    Log(media.TvShows[i].Name + " Last episode: " + lastEpisodeString);
                }
            }

            try
            {
                if (media != null)
                {
                    SaveMedia();
                }

                RestoreSystemCursor();

                if (worker != null)
                {
                    worker.StopImmediately();
                    worker = null;
                }

                if (mouseMoverClientKill)
                {
                    string mouseMoverPath = ConfigurationManager.AppSettings["mouseMoverPath"];
                    if (mouseMoverPath.Contains(".."))
                    {
                        mouseMoverPath = Path.GetFullPath(mouseMoverPath);
                    }
                    Process.Start(mouseMoverPath);
                }

                if (idleMainFormTimer != null)
                {
                    idleMainFormTimer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log("Application end exception: " + ex.Message);
            }
            Log("Application end");
            for (int i = 0; i < 2; i++) Log(Environment.NewLine);
        }

        static public void SaveMedia()
        {
            Log("Saving media...");
            string jsonString = JsonConvert.SerializeObject(media);
            File.WriteAllText(jsonFile, jsonString);
            Log("Media saved");
        }

        static public void CloseButton_Click(object sender, EventArgs e)
        {
            Button closeButton = sender as Button;
            Panel mainPanel = closeButton.Parent as Panel;
            Form currentForm = mainPanel != null ? (Form)mainPanel.Parent : (Form)closeButton.Parent;
            ShowLoadingCursor();
            currentForm.Close();
            if (!currentForm.Name.Equals("MainForm"))
            {
                Fader.FadeOut(dimmerForm, Fader.FadeSpeed.Normal);
            }
            HideLoadingCursor();
        }

        private void CustomScrollbar_Scroll(object sender, EventArgs e)
        {
            mainFormMainPanel.AutoScrollPosition = new Point(0, customScrollbar.Value);
            closeButton.Visible = customScrollbar.Value == 0 ? true : false;
        }

        private void MainFormMainPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            int newVal = -mainFormMainPanel.AutoScrollPosition.Y;
            closeButton.Visible = newVal == 0 ? true : false;
            customScrollbar.Value = newVal;
        }

        private void HeaderLabel_Paint(object sender, PaintEventArgs e)
        {
            float fontSize = GetHeaderFontSize(e.Graphics, this.Bounds.Size, movieLabel.Font, movieLabel.Text);
            float halfFontSize = fontSize / 2;
            Font f = new Font("Arial", fontSize, FontStyle.Bold);
            Font halfF = new Font("Arial", halfFontSize, FontStyle.Bold);
            movieLabel.Font = f;
            tvLabel.Font = f;
            cartoonsLabel.Font = halfF;
        }

        public static float GetHeaderFontSize(Graphics graphics, Size size, Font font, string str)
        {
            SizeF stringSize = graphics.MeasureString(str, font);
            float ratio = (size.Height / stringSize.Height) / 10;
            return font.Size * ratio;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up)
            {
                layoutController.MovePointPosition(layoutController.up);
                return true;
            }
            if (keyData == Keys.Down)
            {
                layoutController.MovePointPosition(layoutController.down);
                return true;
            }
            if (keyData == Keys.Left)
            {
                layoutController.MovePointPosition(layoutController.left);
                return true;
            }
            if (keyData == Keys.Right)
            {
                layoutController.MovePointPosition(layoutController.right);
                return true;
            }
            if (keyData == Keys.Enter)
            {
                MouseWorker.DoMouseClick();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                layoutController.CloseCurrentForm();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public static void Log(string message)
        {
            bool newLine = false;
            if (message == Environment.NewLine)
            {
                newLine = true;
            }
            if (debugLogEnabled)
            {
                using (StreamWriter sw = File.AppendText(debugLogPath))
                {
                    if (newLine)
                    {
                        sw.WriteLine();
                        return;
                    }
                    sw.WriteLine("{0} - {1}: {2}", DateTime.Now.ToString("dd-MM-yy HH:mm:ss.fff"), (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name, message);
                    Debug.WriteLine("{0} - {1}: {2}", DateTime.Now.ToString("dd-MM-yy HH:mm:ss.fff"), (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name, message);
                }
            }
        }

        #endregion

        #region Startup

        private void InitializeGui()
        {
            mainFormMainPanel = new Panel();
            mainFormMainPanel.Size = this.Size;
            mainFormMainPanel.AutoScroll = true;
            mainFormMainPanel.Name = "mainFormMainPanel";
            mainFormMainPanel.MouseWheel += MainFormMainPanel_MouseWheel;
            layoutController.mainFormMainPanel = mainFormMainPanel;

            closeButton.Visible = true;
            closeButton.Location = new Point(mainFormMainPanel.Width - (int)(closeButton.Width * 1.75), (closeButton.Width / 6));
            closeButton.Cursor = blueHandCursor;
            layoutController.mainFormClose = closeButton;

            this.Controls.Add(mainFormMainPanel);

            List<Control> moviePanelList = new List<Control>();
            Panel currentPanel = null;
            int count = 0;
            int panelCount = 0;
            int widthValue = (int)(mainFormMainPanel.Width / 6.12);
            int heightValue = (int)(widthValue * 1.5);

            for (int i = 0; i < media.Movies.Length; i++)
            {
                if (count == 6)
                {
                    count = 0;
                }

                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Black;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "movie" + panelCount;
                    panelCount++;
                    moviePanelList.Add(currentPanel);
                }

                PictureBox movieBox = new PictureBox();
                movieBox.Width = widthValue;
                movieBox.Height = heightValue;
                string imagePath = media.Movies[i].Poster;
                try
                {
                    movieBox.Image = Image.FromFile(imagePath);
                }
                catch
                {
                    movieBox.Image = Properties.Resources.noprev;
                }
                movieBox.BackColor = Color.Black;
                movieBox.Left = movieBox.Width * currentPanel.Controls.Count;
                movieBox.Cursor = blueHandCursor;
                movieBox.SizeMode = PictureBoxSizeMode.StretchImage;
                movieBox.Padding = new Padding(20);
                movieBox.Name = media.Movies[i].Name;
                movieBox.Click += TvForm.MovieBox_Click;
                movieBox.MouseEnter += (s, e) =>
                {
                    layoutController.ClearMovieBoxBorder();
                    movieBox.BorderStyle = BorderStyle.Fixed3D;
                };
                movieBox.MouseLeave += (s, e) =>
                {
                    movieBox.BorderStyle = BorderStyle.None;
                };
                currentPanel.Controls.Add(movieBox);
                layoutController.mainFormControlList.Add(movieBox);
                layoutController.movieBoxes.Add(movieBox);
                count++;
            }

            List<Control> tvPanelList = CreateTvBoxPanels(false);
            List<Control> cartoolPanelList = CreateTvBoxPanels(true);

            movieLabel = new Label();
            movieLabel.Text = "Movies";
            movieLabel.Dock = DockStyle.Top;
            movieLabel.Paint += HeaderLabel_Paint;
            movieLabel.AutoSize = true;
            movieLabel.Name = "movieLabel";

            tvLabel = new Label();
            tvLabel.Text = "TV Shows";
            tvLabel.Dock = DockStyle.Top;
            tvLabel.Paint += HeaderLabel_Paint;
            tvLabel.AutoSize = true;
            tvLabel.Name = "tvLabel";

            cartoonsLabel = new Label();
            cartoonsLabel.Text = " Cartoons";
            cartoonsLabel.Dock = DockStyle.Top;
            cartoonsLabel.Paint += HeaderLabel_Paint;
            cartoonsLabel.AutoSize = true;
            cartoonsLabel.Name = "cartoonsLabel";
            cartoonsLabel.Cursor = blueHandCursor;
            cartoonsLabel.Click += CartoonsLabel_Click;

            moviePanelList.Reverse();
            mainFormMainPanel.Controls.AddRange(moviePanelList.ToArray());
            mainFormMainPanel.Controls.Add(movieLabel);
            cartoolPanelList.Reverse();
            mainFormMainPanel.Controls.AddRange(cartoolPanelList.ToArray());
            mainFormMainPanel.Controls.Add(cartoonsLabel);
            tvPanelList.Reverse();
            mainFormMainPanel.Controls.AddRange(tvPanelList.ToArray());
            mainFormMainPanel.Controls.Add(tvLabel);

            customScrollbar = CustomDialog.CreateScrollBar(mainFormMainPanel);
            customScrollbar.Scroll += CustomScrollbar_Scroll;
            this.Controls.Add(customScrollbar);
            customScrollbar.BringToFront();
            layoutController.mainScrollbar = customScrollbar;
        }

        private List<Control> CreateTvBoxPanels(bool cartoons)
        {
            List<Control> res = new List<Control>();
            Panel currentPanel = null;
            int count = 0;
            int panelCount = 0;
            int widthValue = (int)(mainFormMainPanel.Width / 6.12);
            int heightValue = (int)(widthValue * 1.5);

            for (int i = 0; i < media.TvShows.Length; i++)
            {
                if (cartoons)
                {
                    if (!media.TvShows[i].Cartoon) continue;
                }
                else
                {
                    try
                    {
                        if (media.TvShows[i].Cartoon)
                        {
                            layoutController.numCartoons++;
                            continue;
                        }
                    }
                    catch
                    {
                        CustomDialog.ShowMessage("Error", "Filename does not have correct separator", this.Width, this.Height);
                    }
                }

                if (count == 6)
                {
                    count = 0;
                }

                if (count == 0)
                {
                    currentPanel = new Panel();
                    currentPanel.BackColor = Color.Black;
                    currentPanel.Dock = DockStyle.Top;
                    currentPanel.AutoSize = true;
                    currentPanel.Name = "tv" + panelCount;
                    panelCount++;
                    res.Add(currentPanel);
                }

                PictureBox tvShowBox = new PictureBox();

                tvShowBox.Width = widthValue;
                tvShowBox.Height = heightValue;
                string imagePath = media.TvShows[i].Poster;
                try
                {
                    tvShowBox.Image = Image.FromFile(imagePath);
                }
                catch
                {
                    tvShowBox.Image = Properties.Resources.noprev;
                }
                tvShowBox.BackColor = Color.Black;
                tvShowBox.Left = tvShowBox.Width * currentPanel.Controls.Count;
                tvShowBox.Cursor = blueHandCursor;
                tvShowBox.SizeMode = PictureBoxSizeMode.StretchImage;
                tvShowBox.Padding = new Padding(20);
                tvShowBox.Name = media.TvShows[i].Name;
                tvShowBox.Click += TvForm.TvShowBox_Click;
                tvShowBox.MouseEnter += (s, e) =>
                {
                    layoutController.ClearTvBoxBorder();
                    tvShowBox.BorderStyle = BorderStyle.Fixed3D;
                };
                tvShowBox.MouseLeave += (s, e) =>
                {
                    tvShowBox.BorderStyle = BorderStyle.None;
                };
                currentPanel.Controls.Add(tvShowBox);
                layoutController.mainFormControlList.Add(tvShowBox);
                layoutController.tvBoxes.Add(tvShowBox);
                count++;
            }
            return res;
        }
        private bool CheckForUpdates()
        {
            MediaModel prevMedia = null;

            if (File.Exists(jsonFile))
            {
                string jsonString = File.ReadAllText(jsonFile);
                prevMedia = JsonConvert.DeserializeObject<MediaModel>(jsonString);
            }

            if (prevMedia == null)
            {
                return true;
            }

            bool result = !media.Compare(prevMedia);

            if (!result)
            {
                media = prevMedia;
            }
            else
            {
                File.Copy(jsonFile, jsonFile + ".bak", true);
                media.Ingest(prevMedia);
            }

            return result;
        }


        #endregion

        #region Initializers

        private void InitializeDimmers()
        {
            dimmerForm = new Form();
            dimmerForm.ShowInTaskbar = false;
            dimmerForm.FormBorderStyle = FormBorderStyle.None;
            dimmerForm.BackColor = Color.Black;

            seasonDimmerForm = new Form();
            seasonDimmerForm.ShowInTaskbar = false;
            seasonDimmerForm.FormBorderStyle = FormBorderStyle.None;
            seasonDimmerForm.BackColor = Color.Black;
        }

        private void InitializeMouseWorker()
        {
            Process applicaitionProcess = Process.GetCurrentProcess();
            applicaitionProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

            Process[] p = Process.GetProcessesByName("MouseMoverClient");
            if (p.Length != 0)
            {
                Process.GetProcessesByName("MouseMoverClient")[0].Kill();
                mouseMoverClientKill = true;
            }

            debugLogPath = ConfigurationManager.AppSettings["debugLogPath"] + "lvp-debug.log";
            if (debugLogPath.Contains("%USERPROFILE%"))
            {
                debugLogPath = debugLogPath.Replace("%USERPROFILE%", Environment.GetEnvironmentVariable("USERPROFILE"));
            }
            debugLogEnabled = bool.Parse(ConfigurationManager.AppSettings["debugLogEnabled"]);

            worker = new MouseWorker(this);
            worker.Start();
        }

        private void InitializeIdleTimer()
        {
            idleMainFormTimer = new System.Threading.Timer(mt_ =>
            {
                if (PlayerForm.isPlaying)
                {
                    return;
                }
                TimeSpan t = TimeSpan.FromMinutes(20); //TimeSpan.FromSeconds(10); 
                if (GetIdleTime() > t.TotalMilliseconds)
                {
                    Log("Reached 20 minutes of idle time");
                    this.Invoke(new MethodInvoker(delegate
                    {
                        this.Close();
                    }));
                }
            }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10)); //TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Background worker

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            CacheBuilder cacheBuilder = new CacheBuilder(this);
            string mediaPath = ConfigurationManager.AppSettings["mediaPath"];
            string mediaPathB = ConfigurationManager.AppSettings["mediaPathB"];
            cacheBuilder.ProcessDirectory(mediaPath, mediaPathB);

            if (media == null)
            {
                Log("Media is null");
                throw new ArgumentNullException();
            }

            bool update = CheckForUpdates();
            if (update)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    //To-do: make init here
                    loadingCircle1.Visible = true;
                }));
                cacheBuilder.UpdateLoadingLabel(null);
                Task buildCache = cacheBuilder.BuildCacheAsync();
                buildCache.Wait();
            }
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingLabel.Dispose();
            loadingCircle1.Dispose();
            ShowLoadingCursor();
            this.Padding = new Padding(5, 20, 20, 20);
            layoutController = new LayoutController(media.Count);
            InitializeGui();
            layoutController.Initialize();
            worker.InitializeSerialPort(layoutController);
            foreach (TvShow show in media.TvShows)
            {
                if (show.Cartoon) cartoons.Add(show);
            }
            HideLoadingCursor();
        }

        #endregion

        #region Mouse

        private void RestoreSystemCursor()
        {
            string[] keys = Properties.Resources.keys_backup.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string key in keys)
            {
                string[] keyValuePair = key.Split('=');
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors\", keyValuePair[0], keyValuePair[1]);
            }
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            SystemParametersInfo(0x2029, 0, 32, 0x01);
        }

        private void InitializeCustomCursor()
        {
            string cursorPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            string[] keys = Properties.Resources.keys_custom.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string key in keys)
            {
                string[] keyValuePair = key.Split('=');
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors\", keyValuePair[0], cursorPath + keyValuePair[1]);
            }
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            uint mouseSize = uint.Parse(ConfigurationManager.AppSettings["mouseSize"]);
            SystemParametersInfo(0x2029, 0, mouseSize, 0x01);
        }

        static public void ShowLoadingCursor()
        {
            Cursor.Current = Cursors.WaitCursor;
            if (hideCursor)
            {
                for (int j = 0; j < cursorCount; j++)
                {
                    Cursor.Show();
                }
                cursorCount = 0;
            }
            Cursor.Position = new Point(mainFormSize.Width / 2 - 32, mainFormSize.Height / 2 - 32);
        }

        static public void HideLoadingCursor()
        {
            if (hideCursor)
            {
                Cursor.Hide();
                cursorCount++;
            }
            Cursor.Current = Cursors.Default;
        }

        #endregion

        private void CartoonsLabel_Click(object sender, EventArgs e)
        {
            cartoonShuffle = true;
            cartoonIndex = 0;
            cartoonShuffleList.Clear();
            TvForm.PlayRandomCartoon();
            cartoonShuffle = false;
        }

    }

    public class RoundButton : Button
    {
        // https://stackoverflow.com/questions/3708113/round-shaped-buttons
        protected override void OnPaint(PaintEventArgs e)
        {
            System.Drawing.Drawing2D.GraphicsPath grPath = new System.Drawing.Drawing2D.GraphicsPath();
            grPath.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new Region(grPath);
            base.OnPaint(e);
        }
    }

    public class NoHScrollTree : TreeView
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x8000; // TVS_NOHSCROLL
                return cp;
            }
        }
    }

    public static class StringExtension
    {
        private const string targetSingleQuoteSymbol = "'";
        private const string genericSingleQuoteSymbol = "â€™";
        private const string openSingleQuoteSymbol = "â€˜";
        private const string closeSingleQuoteSymbol = "â€™";
        private const string frenchAccentAigu = "Ã©";
        private const string frenchAccentGrave = "Ã";

        public static string fixBrokenQuotes(this string str)
        {
            return str.Replace(genericSingleQuoteSymbol, targetSingleQuoteSymbol).Replace(openSingleQuoteSymbol, targetSingleQuoteSymbol)
                .Replace(closeSingleQuoteSymbol, targetSingleQuoteSymbol).Replace(frenchAccentAigu, "e").Replace(frenchAccentGrave, "a").Replace("%", "percent");
        }
    }

}