namespace LibmpvIptvClient.Architecture.Platform;

public interface IPlatformAdapter
{
    string Name { get; }
    ISystemApi System { get; }
    IStorageApi Storage { get; }
    INotificationApi Notification { get; }
}

public interface ISystemApi
{
    RuntimeInfo GetRuntimeInfo();
}

public interface IStorageApi
{
    string GetAppDataDirectory();
    string Combine(params string[] paths);
}

public interface INotificationApi
{
    void ShowInfo(string title, string message);
}

public sealed record RuntimeInfo(
    string PlatformName,
    string FrameworkDescription,
    string ProcessArchitecture,
    int ProcessorCount);
