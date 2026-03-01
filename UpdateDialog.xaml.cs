using System;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LibmpvIptvClient
{
    public partial class UpdateDialog : Window
    {
        private readonly AboutWindow.ReleaseInfo? _info;
        private readonly HttpClient _http = new HttpClient();
        private string _downloadPath = "";
        private int _cdnCount = 0;
        private class Mirror
        {
            public string Name { get; set; } = "";
            public string Url { get; set; } = "";
            public override string ToString() => Name;
        }
        public UpdateDialog(AboutWindow.ReleaseInfo? info)
        {
            InitializeComponent();
            _info = info;
            TxtNewVer.Text = info?.Version ?? "-";
            TxtNotes.Text = info?.Notes ?? LibmpvIptvClient.Helpers.ResxLocalizer.Get("Update_NoNotes", "（无更新说明）");
            LoadMirrors(info?.DownloadUrl ?? "");
        }

        private void LoadMirrors(string officialUrl)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(officialUrl))
                {
                    CbMirrors.Items.Add(new Mirror { Name = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Update_Official", "官方"), Url = officialUrl });
                }
                var cdnList = LibmpvIptvClient.AppSettings.Current.UpdateCdnMirrors ?? new System.Collections.Generic.List<string>();
                if (cdnList.Count == 0)
                {
                    var cdnFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "screenshots", "cdn.md");
                    if (File.Exists(cdnFile))
                    {
                        foreach (var line in File.ReadAllLines(cdnFile))
                        {
                            var t = line.Trim();
                            if (t.StartsWith("http")) cdnList.Add(t);
                        }
                        LibmpvIptvClient.AppSettings.Current.UpdateCdnMirrors = cdnList;
                        LibmpvIptvClient.AppSettings.Current.Save();
                    }
                }
                _cdnCount = 0;
                foreach (var u in cdnList)
                {
                    // 把官方URL映射到 CDN 前缀
                    var mapped = !string.IsNullOrWhiteSpace(officialUrl) ? (u.TrimEnd('/') + "/" + officialUrl) : u;
                    _cdnCount++;
                    CbMirrors.Items.Add(new Mirror { Name = _cdnCount == 1 ? LibmpvIptvClient.Helpers.ResxLocalizer.Get("Update_Cdn", "CDN") : $"{LibmpvIptvClient.Helpers.ResxLocalizer.Get("Update_Cdn", "CDN")}-{_cdnCount}", Url = mapped });
                }
                if (CbMirrors.Items.Count > 0) CbMirrors.SelectedIndex = 0;
            }
            catch { }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CbMirrors.SelectedItem == null) return;
                string url = "";
                if (CbMirrors.SelectedItem is Mirror m) url = m.Url;
                else url = CbMirrors.SelectedItem.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(url)) return;
                BtnDownload.IsEnabled = false;
                Pb.Value = 0;
                TxtProgress.Text = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Progress_Preparing", "准备...");

                var tmp = System.IO.Path.GetTempPath();
                var fileName = _info?.FileName;
                if (string.IsNullOrWhiteSpace(fileName)) fileName = "IPTV_Player_Update.exe";
                _downloadPath = System.IO.Path.Combine(tmp, fileName);
                if (File.Exists(_downloadPath)) try { File.Delete(_downloadPath); } catch { }

                var sw = Stopwatch.StartNew();
                var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();
                var total = resp.Content.Headers.ContentLength ?? -1;
                using (var input = await resp.Content.ReadAsStreamAsync())
                using (var output = File.OpenWrite(_downloadPath))
                {
                    var buffer = new byte[81920];
                    int read;
                    long readTotal = 0;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read);
                        readTotal += read;
                        if (total > 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Pb.Maximum = total;
                                Pb.Value = readTotal;
                                var elapsed = sw.Elapsed.TotalSeconds;
                                var speed = elapsed > 0 ? readTotal / elapsed : 0;
                                var doneMb = readTotal / 1024.0 / 1024.0;
                                var totalMb = total / 1024.0 / 1024.0;
                                var speedKb = speed / 1024.0;
                                var percent = total > 0 ? (readTotal * 100.0 / total) : 0;
                                var eta = speed > 0 ? TimeSpan.FromSeconds(Math.Max(0, (total - readTotal) / speed)) : TimeSpan.Zero;
                                var etaLabel = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Progress_ETA", "预计");
                                TxtProgress.Text = $"{doneMb:0.0}/{totalMb:0.0} MB  |  {percent:0}%  |  {speedKb:0.0} KB/s  |  {etaLabel} {eta:mm\\:ss}";
                            }, DispatcherPriority.Background);
                        }
                    }
                }
                TxtProgress.Text += "  |  100% " + LibmpvIptvClient.Helpers.ResxLocalizer.Get("Progress_Complete", "完成");
                BtnInstall.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, string.Format(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_DownloadFailed", "下载失败：\n{0}"), ex.Message), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_UnhandledTitle", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                BtnDownload.IsEnabled = true;
            }
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
            if (!File.Exists(_downloadPath))
            {
                System.Windows.MessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_InstallerNotFound", "未找到安装包，请先下载。"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var r = ModernMessageBox.Show(this,
                LibmpvIptvClient.Helpers.ResxLocalizer.Get("Confirm_InstallUpdate", "即将关闭播放器并启动安装包，是否继续？"),
                LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"),
                MessageBoxButton.YesNo);
                if (r != true) return;
                Process.Start(new ProcessStartInfo(_downloadPath) { UseShellExecute = true });
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, string.Format(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_StartInstallerFailed", "无法启动安装包：\n{0}"), ex.Message), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_UnhandledTitle", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
