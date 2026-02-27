using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace LibmpvIptvClient
{
    public partial class TopOverlay : Window
    {
        public event Action? Minimize;
        public event Action? MaximizeRestore; // Toggles FS or Maximize
        public event Action? CloseWindow;
        public event Action? FullscreenToggle;
        public event Action<bool>? TopmostChanged;
        public event Action? OpenFile;
        public event Action? OpenUrl;
        public event Action? OpenSettings;
        public event Action? AddM3u;
        public event Action? AddM3uFile;
        public event Action? ExitApp;
        public event Action<M3uSource>? LoadM3u;
        public event Action<M3uSource>? EditM3u;
        public event Action<bool>? FccChanged;
        public event Action<bool>? UdpChanged;
        public event Action<bool>? EpgToggled;
        public event Action<bool>? DrawerToggled;
        public Func<bool>? IsUdpEnabled;
        public Func<bool>? IsTopmost;
        public Func<bool>? IsEpgVisible;
        public Func<bool>? IsDrawerVisible;

        public bool IsMenuOpen => BtnTitle.ContextMenu?.IsOpen == true;

        public TopOverlay()
        {
            InitializeComponent();
            Loaded += (s, e) => { if (IsTopmost != null && BtnPin != null) BtnPin.IsChecked = IsTopmost(); };
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e) => Minimize?.Invoke();
        private void BtnMax_Click(object sender, RoutedEventArgs e) => MaximizeRestore?.Invoke();
        private void BtnClose_Click(object sender, RoutedEventArgs e) => CloseWindow?.Invoke();
        private void BtnFullscreen_Click(object sender, RoutedEventArgs e) => FullscreenToggle?.Invoke();
        private void BtnPin_Click(object sender, RoutedEventArgs e) => TopmostChanged?.Invoke(BtnPin.IsChecked == true);

        private void BtnTitle_Click(object sender, RoutedEventArgs e)
        {
            var cm = LibmpvIptvClient.Helpers.MenuBuilder.BuildMainMenu(
                openFile: OpenFile,
                openUrl: OpenUrl,
                addM3uFile: AddM3uFile,
                addM3uUrl: AddM3u,
                editM3u: EditM3u,
                loadM3u: LoadM3u,
                openSettings: OpenSettings,
                showAbout: null, // Not supported or needs separate event
                exitApp: ExitApp,
                toggleFcc: (on) => 
                {
                    AppSettings.Current.FccPrefetchCount = on ? 2 : 0;
                    AppSettings.Current.Save();
                    FccChanged?.Invoke(on);
                },
                toggleUdp: (on) => 
                {
                    AppSettings.Current.EnableUdpOptimization = on;
                    AppSettings.Current.Save();
                    UdpChanged?.Invoke(on);
                },
                toggleEpg: (on) => EpgToggled?.Invoke(on),
                toggleDrawer: (on) => DrawerToggled?.Invoke(on),
                isEpgChecked: IsEpgVisible?.Invoke() ?? false,
                isDrawerChecked: IsDrawerVisible?.Invoke() ?? false
            );

            BtnTitle.ContextMenu = cm;
            BtnTitle.ContextMenu.PlacementTarget = BtnTitle;
            BtnTitle.ContextMenu.IsOpen = true;
        }

        public void SetTitle(string title)
        {
            // BtnTitle.Content = title; // Removed to keep the icon+text layout
        }
    }
}