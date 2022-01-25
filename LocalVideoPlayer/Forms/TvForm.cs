using System.Drawing;
using System.Windows.Forms;

namespace LocalVideoPlayer.Forms
{
    public partial class TvForm : Form
    {
        private Cursor blueHandCursor = new Cursor(Properties.Resources.blue_link.Handle);
        public TvForm()
        {
            InitializeComponent();
            closeButton.Cursor = blueHandCursor;
        }

        private void TvForm_Load(object sender, System.EventArgs e)
        {
            loadingCircle1.Location = new Point(this.Width / 2 - loadingCircle1.Width / 2, this.Height / 2 - loadingCircle1.Height / 2);
        }

        internal void ShowLoadingCircle()
        {
            loadingCircle1.Visible = true;
            loadingCircle1.Active = true;
        }

        internal void HideLoadingCircle()
        {
            loadingCircle1.Visible = false;
            loadingCircle1.Active = false;
        }
    }
}
