using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace LibmpvIptvClient.Helpers
{
    public static class MenuBuilder
    {
        public static ContextMenu BuildMainMenu(
            Action? openFile,
            Action? openUrl,
            Action? addM3uFile,
            Action? addM3uUrl,
            Action<M3uSource>? editM3u,
            Action<M3uSource>? loadM3u,
            Action? openSettings,
            Action? showAbout,
            Action? exitApp,
            Action<bool>? toggleFcc,
            Action<bool>? toggleUdp,
            Action<bool>? toggleEpg,
            Action<bool>? toggleDrawer,
            bool isEpgChecked,
            bool isDrawerChecked)
        {
            var cm = new ContextMenu();

            // 1. File (Open)
            var miFile = new MenuItem { Header = "打开" };
            var miOpenFile = new MenuItem { Header = "打开文件..." };
            miOpenFile.Click += (s, args) => openFile?.Invoke();
            miFile.Items.Add(miOpenFile);

            var miOpenUrl = new MenuItem { Header = "打开链接..." };
            miOpenUrl.Click += (s, args) => openUrl?.Invoke();
            miFile.Items.Add(miOpenUrl);
            cm.Items.Add(miFile);

            // 2. M3U Management
            var miM3u = new MenuItem { Header = "M3U" };
            var miAddFile = new MenuItem { Header = "添加 M3U 文件..." };
            miAddFile.Click += (s, args) => addM3uFile?.Invoke();
            miM3u.Items.Add(miAddFile);

            var miAddUrl = new MenuItem { Header = "添加 M3U 地址..." };
            miAddUrl.Click += (s, args) => addM3uUrl?.Invoke();
            miM3u.Items.Add(miAddUrl);

            miM3u.Items.Add(new Separator());

            var miEditM3u = new MenuItem { Header = "编辑 M3U" };
            if (AppSettings.Current.SavedSources != null && AppSettings.Current.SavedSources.Count > 0)
            {
                foreach (var src in AppSettings.Current.SavedSources)
                {
                    var miSrc = new MenuItem { Header = src.Name };
                    miSrc.Tag = src;
                    miSrc.Click += (s, args) => 
                    {
                        if (s is MenuItem m && m.Tag is M3uSource source)
                            editM3u?.Invoke(source);
                    };
                    miEditM3u.Items.Add(miSrc);
                }
            }
            else
            {
                miEditM3u.IsEnabled = false;
            }
            miM3u.Items.Add(miEditM3u);
            cm.Items.Add(miM3u);

            // 3. Performance Settings
            var miPerf = new MenuItem { Header = "性能" };
            var miFcc = new MenuItem 
            { 
                Header = "FCC (快速切台)", 
                IsCheckable = true, 
                IsChecked = AppSettings.Current.FccPrefetchCount > 0 
            };
            miFcc.Click += (s, args) => toggleFcc?.Invoke(miFcc.IsChecked);
            miPerf.Items.Add(miFcc);

            var miUdp = new MenuItem 
            { 
                Header = "UDP (组播优化)", 
                IsCheckable = true, 
                IsChecked = AppSettings.Current.EnableUdpOptimization
            };
            miUdp.Click += (s, args) => toggleUdp?.Invoke(miUdp.IsChecked);
            miPerf.Items.Add(miUdp);
            cm.Items.Add(miPerf);

            // 4. List Management
            var miList = new MenuItem { Header = "列表" };
            var miEpg = new MenuItem 
            { 
                Header = "EPG", 
                IsCheckable = true, 
                IsChecked = isEpgChecked
            };
            miEpg.Click += (s, args) => toggleEpg?.Invoke(miEpg.IsChecked);
            miList.Items.Add(miEpg);

            var miChannel = new MenuItem 
            { 
                Header = "频道", 
                IsCheckable = true, 
                IsChecked = isDrawerChecked
            };
            miChannel.Click += (s, args) => toggleDrawer?.Invoke(miChannel.IsChecked);
            miList.Items.Add(miChannel);

            var miM3uList = new MenuItem { Header = "切换 M3U" };
            if (AppSettings.Current.SavedSources != null && AppSettings.Current.SavedSources.Count > 0)
            {
                foreach (var src in AppSettings.Current.SavedSources)
                {
                    var miSrc = new MenuItem { Header = src.Name };
                    miSrc.Tag = src;
                    miSrc.IsCheckable = true;
                    miSrc.IsChecked = src.IsSelected;
                    miSrc.Click += (s, args) => 
                    {
                        if (s is MenuItem m && m.Tag is M3uSource source)
                            loadM3u?.Invoke(source);
                    };
                    miM3uList.Items.Add(miSrc);
                }
            }
            else
            {
                miM3uList.IsEnabled = false;
            }
            miList.Items.Add(miM3uList);
            cm.Items.Add(miList);

            cm.Items.Add(new Separator());

            // 5. System
            var miSystem = new MenuItem { Header = "系统" };
            var miSettings = new MenuItem { Header = "设置" };
            miSettings.Click += (s, args) => openSettings?.Invoke();
            miSystem.Items.Add(miSettings);

            var miAbout = new MenuItem { Header = "关于" };
            miAbout.Click += (s, args) => showAbout?.Invoke();
            miSystem.Items.Add(miAbout);

            var miExit = new MenuItem { Header = "退出" };
            miExit.Click += (s, args) => exitApp?.Invoke();
            miSystem.Items.Add(miExit);
            cm.Items.Add(miSystem);

            return cm;
        }
    }
}
