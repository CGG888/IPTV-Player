using System;
using System.Windows;

namespace LibmpvIptvClient.Architecture.Presentation.View
{
    public class MainWindowFullscreenInputManager
    {
        private readonly MainWindow _window;

        public MainWindowFullscreenInputManager(MainWindow window)
        {
            _window = window;
        }

        public void FsHost_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            _window.AdjustVolumeByWheelFromManager(e.Delta);
            _window.ShowOverlayWithDelayFromManager();
            e.Handled = true;
        }

        public void FsPanel_MouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            _window.AdjustVolumeByWheelFromManager(e.Delta);
            _window.ShowOverlayWithDelayFromManager();
        }

        public void FsPanel_MouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Right) return;
            try
            {
                _window.Dispatcher.Invoke(() =>
                {
                    var cm = _window.MenuManagerForManager?.CreateAppMenu();
                    if (cm != null)
                    {
                        cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                        cm.IsOpen = true;
                    }
                });
            }
            catch { }
        }

        public void FsPanel_DoubleClick(object? sender, EventArgs e)
        {
            _window.ToggleFullscreenFromManager(false);
        }
    }
}
