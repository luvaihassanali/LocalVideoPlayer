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
    }
}
