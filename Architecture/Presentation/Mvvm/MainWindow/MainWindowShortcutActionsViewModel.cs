using System.Windows.Input;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public enum MainWindowShortcutAction
{
    None,
    TogglePlayPause,
    SeekBackward,
    SeekForward,
    OpenDebug
}

public sealed class MainWindowShortcutActionsViewModel : ViewModelBase
{
    private readonly MainShellViewModel _shell;

    public event System.Action? RequestDebugWindow;

    public MainWindowShortcutActionsViewModel(MainShellViewModel shell)
    {
        _shell = shell;
    }

    public MainWindowShortcutAction ResolveAction(Key key)
    {
        return key switch
        {
            Key.Space => MainWindowShortcutAction.TogglePlayPause,
            Key.Left => MainWindowShortcutAction.SeekBackward,
            Key.Right => MainWindowShortcutAction.SeekForward,
            Key.F1 => MainWindowShortcutAction.OpenDebug,
            _ => MainWindowShortcutAction.None
        };
    }

    public void ExecuteAction(MainWindowShortcutAction action)
    {
        switch (action)
        {
            case MainWindowShortcutAction.TogglePlayPause:
                if (_shell.PlaybackActions.TryTogglePlayPause(_shell.PlayerEngine, _shell.IsPaused, out var next))
                {
                    _shell.IsPaused = next;
                }
                break;
            case MainWindowShortcutAction.SeekBackward:
                // 仅回看模式可用
                if (_shell.CurrentPlayingProgram != null)
                {
                    _shell.PlaybackActions.TrySeekRelative(_shell.PlayerEngine, -10);
                }
                break;
            case MainWindowShortcutAction.SeekForward:
                // 仅回看模式可用
                if (_shell.CurrentPlayingProgram != null)
                {
                    _shell.PlaybackActions.TrySeekRelative(_shell.PlayerEngine, 10);
                }
                break;
            case MainWindowShortcutAction.OpenDebug:
                RequestDebugWindow?.Invoke();
                break;
        }
    }
}
