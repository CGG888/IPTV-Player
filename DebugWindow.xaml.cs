using System;
using System.Windows;
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
        }
        void OnLog(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.AppendText(msg + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });
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
