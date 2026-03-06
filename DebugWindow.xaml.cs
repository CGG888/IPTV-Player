using System;
using System.Windows;
using System.Windows.Input;
using LibmpvIptvClient.Diagnostics;

namespace LibmpvIptvClient
{
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
            Logger.OnMessage += OnLog;
            Closed += OnClosed;
            PreviewKeyDown += OnPreviewKeyDown;
        }
        void OnLog(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.AppendText(msg + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });
        }
        void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.F1)
                {
                    Close();
                    e.Handled = true;
                }
            }
            catch { }
        }
        void OnClosed(object? sender, EventArgs e)
        {
            Logger.OnMessage -= OnLog;
        }
        void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }
    }
}
