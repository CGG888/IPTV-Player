using System.Collections.Generic;
using System.Windows;
using LibmpvIptvClient.Models;
using LibmpvIptvClient.Services;

namespace LibmpvIptvClient.Architecture.Presentation.View
{
    public class MainWindowHistoryManager
    {
        private readonly MainWindow _window;
        private readonly Architecture.Presentation.Mvvm.MainWindow.MainShellViewModel _shell;
        private readonly UserDataStore _userDataStore;

        public MainWindowHistoryManager(
            MainWindow window,
            Architecture.Presentation.Mvvm.MainWindow.MainShellViewModel shell,
            UserDataStore userDataStore)
        {
            _window = window;
            _shell = shell;
            _userDataStore = userDataStore;
        }

        public void ListHistory_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (_window.ListHistory.SelectedItem is HistoryItem hi)
                {
                    _shell.HistoryActions.HandleDoubleClick(
                        hi,
                        _shell.Channels,
                        ch => _shell.ChannelPlaybackActions.PlayChannel(ch, _window.ListEpg.ItemsSource as IEnumerable<EpgProgram>),
                        (url, playTypeLabel) => _shell.ChannelPlaybackActions.PlayUrlGeneric(url, playTypeLabel));
                }
            }
            catch { }
        }

        public void HistoryDeleteOne_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.Parameter is HistoryItem hi)
                {
                    var list = _shell.HistoryActions.DeleteOne(_userDataStore, hi);
                    _window.ListHistory.ItemsSource = list;
                    e.Handled = true;
                }
            }
            catch { }
        }

        public void HistoryDeleteOne_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is HistoryItem;
            e.Handled = true;
        }

        public void BtnHistoryDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var list = _shell.HistoryActions.DeleteSelected(_userDataStore, _window.ListHistory.SelectedItems);
                _window.ListHistory.ItemsSource = list;
            }
            catch { }
        }

        public void BtnHistoryClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var list = _shell.HistoryActions.ClearAll(_userDataStore);
                _window.ListHistory.ItemsSource = list;
            }
            catch { }
        }
    }
}
