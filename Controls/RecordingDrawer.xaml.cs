using System;
using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace LibmpvIptvClient.Controls
{
    public partial class RecordingDrawer : UserControl
    {
        private RecordingConfig _config = new RecordingConfig();
        public RecordingDrawer()
        {
            InitializeComponent();
        }
        public void Load(RecordingConfig config)
        {
            _config = config ?? new RecordingConfig();
            CbEnabled.IsChecked = _config.Enabled;
            SelectByTag(CbPlayChoice, _config.DefaultPlayChoice);
            SelectByTag(CbSaveMode, _config.SaveMode);
            TbDirTemplate.Text = _config.DirTemplate;
            TbFileTemplate.Text = _config.FileTemplate;
            CbVerifyDir.IsChecked = _config.VerifyDirReady;
            TbGrowthTimeout.Text = Math.Max(1, _config.GrowthTimeoutSec).ToString();
            TbRetryCount.Text = Math.Max(0, _config.RetryCount).ToString();
            TbUploadCc.Text = Math.Max(1, _config.UploadMaxConcurrency).ToString();
            TbUploadRetry.Text = Math.Max(0, _config.UploadRetry).ToString();
            TbUploadKbps.Text = Math.Max(0, _config.UploadMaxKBps).ToString();
            CbResume.IsChecked = _config.ResumeUpload;
            try { TbRealtimeInterval.Text = Math.Max(2, _config.RealtimeUploadIntervalSec).ToString(); } catch { }
            try { TbRemoteTempSuffix.Text = string.IsNullOrWhiteSpace(_config.RemoteTempSuffix) ? ".part" : _config.RemoteTempSuffix; } catch { }
            try { CbFinalize.IsChecked = _config.RealtimeFinalizeEnabled; } catch { }
            try { TbFinalizeDelay.Text = Math.Max(0, _config.RealtimeFinalizeDelaySec).ToString(); } catch { }
            try { TbFinalizeKbps.Text = Math.Max(0, _config.RealtimeFinalizeMaxKBps).ToString(); } catch { }
        }
        public void Save(RecordingConfig config)
        {
            config.Enabled = CbEnabled.IsChecked == true;
            config.DefaultPlayChoice = GetSelectedTag(CbPlayChoice);
            config.SaveMode = GetSelectedTag(CbSaveMode);
            config.DirTemplate = TbDirTemplate.Text ?? "";
            config.FileTemplate = TbFileTemplate.Text ?? "";
            config.VerifyDirReady = CbVerifyDir.IsChecked == true;
            if (int.TryParse(TbGrowthTimeout.Text, out var gt)) config.GrowthTimeoutSec = Math.Max(1, gt);
            if (int.TryParse(TbRetryCount.Text, out var rc)) config.RetryCount = Math.Max(0, rc);
            if (int.TryParse(TbUploadCc.Text, out var cc)) config.UploadMaxConcurrency = Math.Max(1, cc);
            if (int.TryParse(TbUploadRetry.Text, out var ur)) config.UploadRetry = Math.Max(0, ur);
            if (int.TryParse(TbUploadKbps.Text, out var kb)) config.UploadMaxKBps = Math.Max(0, kb);
            config.ResumeUpload = CbResume.IsChecked == true;
            if (int.TryParse(TbRealtimeInterval.Text, out var iv)) config.RealtimeUploadIntervalSec = Math.Max(2, iv);
            config.RemoteTempSuffix = string.IsNullOrWhiteSpace(TbRemoteTempSuffix.Text) ? ".part" : TbRemoteTempSuffix.Text.Trim();
            config.RealtimeFinalizeEnabled = CbFinalize.IsChecked == true;
            if (int.TryParse(TbFinalizeDelay.Text, out var fd)) config.RealtimeFinalizeDelaySec = Math.Max(0, fd);
            if (int.TryParse(TbFinalizeKbps.Text, out var fk)) config.RealtimeFinalizeMaxKBps = Math.Max(0, fk);
        }
        void BtnOpenUploadQueue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new UploadQueueWindow();
                win.Owner = Window.GetWindow(this);
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.Show();
            }
            catch { }
        }
        void SelectByTag(System.Windows.Controls.ComboBox cb, string tag)
        {
            try
            {
                for (int i = 0; i < cb.Items.Count; i++)
                {
                    if (cb.Items[i] is System.Windows.Controls.ComboBoxItem item)
                    {
                        var t = (item.Tag ?? "").ToString() ?? "";
                        if (string.Equals(t, tag ?? "", StringComparison.OrdinalIgnoreCase))
                        {
                            cb.SelectedIndex = i; return;
                        }
                    }
                }
                cb.SelectedIndex = 0;
            }
            catch { cb.SelectedIndex = 0; }
        }
        string GetSelectedTag(System.Windows.Controls.ComboBox cb)
        {
            try
            {
                if (cb?.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    return (item.Tag ?? "").ToString() ?? "";
                }
            }
            catch { }
            return "";
        }
    }
}
