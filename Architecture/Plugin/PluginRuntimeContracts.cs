using LibmpvIptvClient.Architecture.Core;

namespace LibmpvIptvClient.Architecture.Plugin;

public sealed record PluginDescriptor(
    PluginManifest Manifest,
    string PluginDirectory,
    string ManifestPath);

public interface IPluginManager : IAsyncDisposable
{
    IReadOnlyCollection<PluginDescriptor> AvailablePlugins { get; }
    IReadOnlyCollection<IPluginModule> LoadedPlugins { get; }
    Task ScanAsync(CancellationToken cancellationToken);
    Task LoadAsync(string pluginId, CancellationToken cancellationToken);
    Task UnloadAsync(string pluginId, CancellationToken cancellationToken);
    event Action<PluginDescriptor>? PluginDiscovered;
    event Action<string>? PluginHotReloaded;
}
