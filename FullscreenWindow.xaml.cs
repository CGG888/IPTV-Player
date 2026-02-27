using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Forms;

namespace LibmpvIptvClient
{
    public partial class FullscreenWindow : Window
    {
        public Panel VideoPanel => FsPanel;
        public WindowsFormsHost Host => FsHost;
        public event System.Action? ExitRequested;
        public FullscreenWindow()
        {
            InitializeComponent();
        }
        void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                ExitRequested?.Invoke();
                e.Handled = true;
            }
        }
    }
}
