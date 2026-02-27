using System.Windows;
using System.Text;
using System;
using System.Runtime.InteropServices;
using LibmpvIptvClient.Diagnostics;

namespace LibmpvIptvClient
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                try { Logger.Log("未处理异常 " + e.Exception?.ToString()); } catch { }
                System.Windows.MessageBox.Show(e.Exception?.Message ?? "Unknown error", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { Logger.Log("未处理异常 " + e.ExceptionObject?.ToString()); } catch { }
                System.Windows.MessageBox.Show(e.ExceptionObject?.ToString() ?? "Unknown error", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch { }
        }
    }
}
