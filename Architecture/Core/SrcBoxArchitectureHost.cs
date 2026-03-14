namespace LibmpvIptvClient.Architecture.Core;

public static class SrcBoxArchitectureHost
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static SrcBoxKernel? _kernel;

    public static SrcBoxKernel Kernel
    {
        get
        {
            if (_kernel is null)
            {
                throw new InvalidOperationException("架构内核尚未初始化");
            }

            return _kernel;
        }
    }

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_kernel is not null)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_kernel is null)
            {
                _kernel = new SrcBoxKernel();
                await _kernel.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async Task ShutdownAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_kernel is not null)
            {
                await _kernel.DisposeAsync().ConfigureAwait(false);
                _kernel = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
