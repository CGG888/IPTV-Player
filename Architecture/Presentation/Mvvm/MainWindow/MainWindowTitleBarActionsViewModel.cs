using System.Windows;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record TitleBarPinState(bool Topmost, bool? IsChecked);

public sealed class MainWindowTitleBarActionsViewModel : ViewModelBase
{
    public TitleBarPinState TogglePin(bool currentTopmost)
    {
        var next = !currentTopmost;
        return new TitleBarPinState(next, next);
    }

    public WindowState ToggleMaximize(WindowState current)
    {
        return current == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
