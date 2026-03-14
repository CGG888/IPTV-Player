using System.Collections.Concurrent;
using System.Diagnostics;

namespace LibmpvIptvClient.Architecture.Tooling;

public sealed class PerformanceProfiler
{
    private readonly ConcurrentDictionary<string, Stopwatch> _running = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TimeSpan> _lastElapsed = new(StringComparer.OrdinalIgnoreCase);

    public void Start(string scopeName)
    {
        var sw = Stopwatch.StartNew();
        _running[scopeName] = sw;
    }

    public TimeSpan Stop(string scopeName)
    {
        if (!_running.TryRemove(scopeName, out var sw))
        {
            return TimeSpan.Zero;
        }

        sw.Stop();
        _lastElapsed[scopeName] = sw.Elapsed;
        return sw.Elapsed;
    }

    public IReadOnlyDictionary<string, TimeSpan> Snapshot()
    {
        return new Dictionary<string, TimeSpan>(_lastElapsed);
    }
}
