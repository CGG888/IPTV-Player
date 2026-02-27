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
                    // Use a more descriptive text if possible, or just the URL
                    // For now, hardcoded "GitHub Repository" in XAML or we can change it dynamically
                    if (linkUrl.Contains("github.com"))
                    {
                        var firstInline = hl.Inlines.FirstInline;
                        if (firstInline is System.Windows.Documents.Run run)
                        {
                            run.Text = "访问 GitHub 项目主页";
                        }
                        else if (firstInline is System.Windows.Documents.InlineUIContainer uiContainer && uiContainer.Child is System.Windows.Controls.TextBlock tb)
                        {
                            tb.Text = "访问 GitHub 项目主页";
                        }
                    }
                }
            }

            if (buttons == MessageBoxButton.YesNo)
            {
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                BtnYes.Content = "是";
                BtnNo.Content = "否";
            }
            else if (buttons == MessageBoxButton.OKCancel)
            {
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                BtnYes.Content = "确定";
                BtnNo.Content = "取消";
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
