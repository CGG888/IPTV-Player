using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Forms;
using System.Windows.Interop;

namespace LibmpvIptvClient
{
    public partial class FullscreenWindow : Window
    {
        public Panel VideoPanel => FsPanel;
        public WindowsFormsHost Host => FsHost;
        public event System.Action? ExitRequested;
        public event System.Action? PlayPauseRequested;
        public event System.Action<int>? SeekRequested; // -1 left, +1 right
        public FullscreenWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInit;
        }
        HwndSourceHook? _hook;
        void OnSourceInit(object? sender, System.EventArgs e)
        {
            var src = (HwndSource)PresentationSource.FromVisual(this);
            _hook = new HwndSourceHook(WndProc);
            src.AddHook(_hook);
        }
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104;
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                int vk = wParam.ToInt32();
                if (vk == 0x1B)
                {
                    ExitRequested?.Invoke();
                    handled = true;
                }
                else if (vk == 0x20)
                {
                    PlayPauseRequested?.Invoke();
                    handled = true;
                }
                else if (vk == 0x25)
                {
                    SeekRequested?.Invoke(-1);
                    handled = true;
                }
                else if (vk == 0x27)
                {
                    SeekRequested?.Invoke(1);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
        void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                ExitRequested?.Invoke();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Space)
            {
                PlayPauseRequested?.Invoke();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                SeekRequested?.Invoke(-1);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Right)
            {
                SeekRequested?.Invoke(1);
                e.Handled = true;
            }
        }
    }
}
