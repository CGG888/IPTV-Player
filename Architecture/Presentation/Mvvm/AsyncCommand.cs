using System.Windows.Input;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm;

public sealed class AsyncCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly CancellationTokenSource _cts = new();
    private bool _isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_isRunning && (canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await execute(_cts.Token).ConfigureAwait(false);
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    public void Cancel() => _cts.Cancel();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
