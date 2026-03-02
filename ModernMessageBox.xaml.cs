using System;
using System.Windows;
using System.Diagnostics;

namespace LibmpvIptvClient
{
    public partial class ModernMessageBox : Window
    {
        public bool Result { get; private set; } = false;

        public ModernMessageBox(string title, string message, MessageBoxButton buttons = MessageBoxButton.OK, string? linkUrl = null)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;

            if (!string.IsNullOrEmpty(linkUrl))
            {
                TxtLink.Visibility = Visibility.Visible;
                if (TxtLink.Inlines.FirstInline is System.Windows.Documents.Hyperlink hl)
                {
                    hl.NavigateUri = new Uri(linkUrl);
                    if (linkUrl.Contains("github.com"))
                    {
                        var firstInline = hl.Inlines.FirstInline;
                        var linkText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Link_GitHubRepo", "访问 GitHub 项目主页");
                        if (firstInline is System.Windows.Documents.Run run)
                        {
                            run.Text = linkText;
                        }
                        else if (firstInline is System.Windows.Documents.InlineUIContainer uiContainer && uiContainer.Child is System.Windows.Controls.TextBlock tb)
                        {
                            tb.Text = linkText;
                        }
                    }
                }
            }

            if (buttons == MessageBoxButton.YesNo)
            {
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                BtnYes.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Btn_Yes", "是");
                BtnNo.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Btn_No", "否");
            }
            else if (buttons == MessageBoxButton.OKCancel)
            {
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                BtnYes.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_OK", "确定");
                BtnNo.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Cancel", "取消");
            }
            else
            {
                BtnOk.Visibility = Visibility.Visible;
            }
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch { }
        }

        public static bool? Show(Window? owner, string message, string title, MessageBoxButton buttons = MessageBoxButton.OK, string? linkUrl = null)
        {
            var dlg = new ModernMessageBox(title, message, buttons, linkUrl);
            if (owner != null)
            {
                dlg.Owner = owner;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                // Inherit topmost from owner if possible, or force it if owner is topmost
                if (owner.Topmost) dlg.Topmost = true;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dlg.Topmost = true; // Default to topmost if no owner
            }
            
            return dlg.ShowDialog();
        }
    }
}
