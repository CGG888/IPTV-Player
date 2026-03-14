using LibmpvIptvClient.Architecture.Core;

namespace LibmpvIptvClient.Architecture.Platform;

public interface IPlatformAdapterFactory
{
    RuntimePlatform DetectPlatform();
    IPlatformAdapter CreateAdapter(RuntimePlatform platform);
}

public sealed class PlatformAdapterFactory : IPlatformAdapterFactory
{
    private readonly Dictionary<RuntimePlatform, Func<IPlatformAdapter>> _builders = new();

    public PlatformAdapterFactory()
    {
        _builders[RuntimePlatform.Windows] = () => new WindowsPlatformAdapter();
        _builders[RuntimePlatform.MacOS] = () => new GenericPlatformAdapter("macOS");
        _builders[RuntimePlatform.Android] = () => new GenericPlatformAdapter("Android");
        _builders[RuntimePlatform.IOS] = () => new GenericPlatformAdapter("iOS");
        _builders[RuntimePlatform.Unknown] = () => new GenericPlatformAdapter("Unknown");
    }

    public RuntimePlatform DetectPlatform()
    {
        if (OperatingSystem.IsWindows()) return RuntimePlatform.Windows;
        if (OperatingSystem.IsMacOS()) return RuntimePlatform.MacOS;
        if (OperatingSystem.IsAndroid()) return RuntimePlatform.Android;
        if (OperatingSystem.IsIOS()) return RuntimePlatform.IOS;
        return RuntimePlatform.Unknown;
    }

    public IPlatformAdapter CreateAdapter(RuntimePlatform platform)
    {
        if (_builders.TryGetValue(platform, out var builder))
        {
            return builder();
        }

        return new GenericPlatformAdapter("Unknown");
    }
}
