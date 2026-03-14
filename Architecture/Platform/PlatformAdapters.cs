using System.Runtime.InteropServices;
using LibmpvIptvClient.Services;
using System.IO;

namespace LibmpvIptvClient.Architecture.Platform;

public sealed class WindowsPlatformAdapter : IPlatformAdapter
{
    public string Name => "Windows";
    public ISystemApi System { get; } = new DefaultSystemApi("Windows");
    public IStorageApi Storage { get; } = new DefaultStorageApi();
    public INotificationApi Notification { get; } = new WindowsNotificationApi();
}

public sealed class GenericPlatformAdapter : IPlatformAdapter
{
    public GenericPlatformAdapter(string name)
    {
        Name = name;
        System = new DefaultSystemApi(name);
        Storage = new DefaultStorageApi();
        Notification = new SilentNotificationApi();
    }

    public string Name { get; }
    public ISystemApi System { get; }
    public IStorageApi Storage { get; }
    public INotificationApi Notification { get; }
}

public sealed class DefaultSystemApi(string platformName) : ISystemApi
{
    public RuntimeInfo GetRuntimeInfo()
    {
        return new RuntimeInfo(
            platformName,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.ProcessorCount);
    }
}

public sealed class DefaultStorageApi : IStorageApi
{
    public string GetAppDataDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    public string Combine(params string[] paths)
    {
        return Path.Combine(paths);
    }
}

public sealed class WindowsNotificationApi : INotificationApi
{
    public void ShowInfo(string title, string message)
    {
        try
        {
            NotificationService.Instance.Show(title, message, timeoutMs: 4000);
        }
        catch
        {
        }
    }
}

public sealed class SilentNotificationApi : INotificationApi
{
    public void ShowInfo(string title, string message)
    {
    }
}
