using System.Reflection;
using System.Text.Json;
using System.IO;
using LibmpvIptvClient.Architecture.Core;
using LibmpvIptvClient.Architecture.Platform;
using LibmpvIptvClient.Architecture.Presentation.Html;

namespace LibmpvIptvClient.Architecture.Plugin;

public sealed class PluginManager : IPluginManager
{
    private readonly string _pluginsRoot;
    private readonly ServiceRegistry _services;
    private readonly IPlatformAdapter _platformAdapter;
    private readonly RuntimePlatform _runtimePlatform;
    private readonly IHtmlCssUiEngine _uiEngine;
    private readonly IVersionPolicy _versionPolicy;
    private readonly Version _hostVersion;
    private readonly Dictionary<string, PluginDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginRuntimeHandle> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileSystemWatcher _watcher;

    public PluginManager(
        string pluginsRoot,
        ServiceRegistry services,
        IPlatformAdapter platformAdapter,
        RuntimePlatform runtimePlatform,
        IHtmlCssUiEngine uiEngine,
        IVersionPolicy versionPolicy,
        Version hostVersion)
    {
        _pluginsRoot = pluginsRoot;
        _services = services;
        _platformAdapter = platformAdapter;
        _runtimePlatform = runtimePlatform;
        _uiEngine = uiEngine;
        _versionPolicy = versionPolicy;
        _hostVersion = hostVersion;

        Directory.CreateDirectory(_pluginsRoot);
        _watcher = new FileSystemWatcher(_pluginsRoot, "plugin.json")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };
        _watcher.Changed += (_, e) => OnPluginManifestChanged(e.FullPath);
        _watcher.Created += (_, e) => OnPluginManifestChanged(e.FullPath);
        _watcher.Deleted += (_, e) => OnPluginManifestChanged(e.FullPath);
        _watcher.Renamed += (_, e) => OnPluginManifestChanged(e.FullPath);
    }

    public IReadOnlyCollection<PluginDescriptor> AvailablePlugins => _descriptors.Values.ToArray();
    public IReadOnlyCollection<IPluginModule> LoadedPlugins => _loaded.Values.Select(v => v.Module).ToArray();
    public event Action<PluginDescriptor>? PluginDiscovered;
    public event Action<string>? PluginHotReloaded;

    public async Task ScanAsync(CancellationToken cancellationToken)
    {
        var manifests = Directory.GetFiles(_pluginsRoot, "plugin.json", SearchOption.AllDirectories);
        foreach (var manifestPath in manifests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptor = await ReadDescriptorAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            if (descriptor is null)
            {
                continue;
            }

            _descriptors[descriptor.Manifest.Id] = descriptor;
            PluginDiscovered?.Invoke(descriptor);
        }
    }

    public async Task LoadAsync(string pluginId, CancellationToken cancellationToken)
    {
        if (_loaded.ContainsKey(pluginId))
        {
            return;
        }

        if (!_descriptors.TryGetValue(pluginId, out var descriptor))
        {
            throw new InvalidOperationException($"未找到插件: {pluginId}");
        }

        var minHostVersion = Version.Parse(descriptor.Manifest.MinHostVersion);
        if (!_versionPolicy.IsCompatible(_hostVersion, minHostVersion))
        {
            throw new InvalidOperationException($"插件 {pluginId} 与主程序版本不兼容");
        }

        foreach (var dependency in descriptor.Manifest.Dependencies)
        {
            if (!_loaded.ContainsKey(dependency))
            {
                await LoadAsync(dependency, cancellationToken).ConfigureAwait(false);
            }
        }

        var pluginAssemblyPath = Path.Combine(descriptor.PluginDirectory, descriptor.Manifest.AssemblyFile);
        var loadContext = new PluginLoadContext(descriptor.PluginDirectory);
        var assembly = loadContext.LoadFromAssemblyPath(pluginAssemblyPath);
        var pluginType = assembly.GetType(descriptor.Manifest.EntryType, throwOnError: true);
        var instance = Activator.CreateInstance(pluginType!) as IPluginModule
            ?? throw new InvalidOperationException($"插件入口类型无效: {descriptor.Manifest.EntryType}");

        var context = new PluginContext(_services, _platformAdapter, _uiEngine, _runtimePlatform);
        await instance.InitializeAsync(context, cancellationToken).ConfigureAwait(false);
        await instance.StartAsync(cancellationToken).ConfigureAwait(false);
        _loaded[pluginId] = new PluginRuntimeHandle(loadContext, descriptor, instance);
    }

    public async Task UnloadAsync(string pluginId, CancellationToken cancellationToken)
    {
        if (!_loaded.TryGetValue(pluginId, out var handle))
        {
            return;
        }

        await handle.Module.StopAsync(cancellationToken).ConfigureAwait(false);
        await handle.Module.DisposeAsync().ConfigureAwait(false);
        _loaded.Remove(pluginId);
        handle.LoadContext.Unload();
        PluginHotReloaded?.Invoke(pluginId);
    }

    public async ValueTask DisposeAsync()
    {
        _watcher.Dispose();
        foreach (var pluginId in _loaded.Keys.ToArray())
        {
            await UnloadAsync(pluginId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void OnPluginManifestChanged(string manifestPath)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var descriptor = await ReadDescriptorAsync(manifestPath, CancellationToken.None).ConfigureAwait(false);
                if (descriptor is null)
                {
                    return;
                }

                _descriptors[descriptor.Manifest.Id] = descriptor;
                if (_loaded.ContainsKey(descriptor.Manifest.Id))
                {
                    await UnloadAsync(descriptor.Manifest.Id, CancellationToken.None).ConfigureAwait(false);
                    await LoadAsync(descriptor.Manifest.Id, CancellationToken.None).ConfigureAwait(false);
                    PluginHotReloaded?.Invoke(descriptor.Manifest.Id);
                }
            }
            catch
            {
            }
        });
    }

    private static async Task<PluginDescriptor?> ReadDescriptorAsync(string manifestPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<PluginManifest>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
        {
            return null;
        }

        var dir = Path.GetDirectoryName(manifestPath) ?? string.Empty;
        return new PluginDescriptor(manifest, dir, manifestPath);
    }

    private sealed record PluginRuntimeHandle(
        PluginLoadContext LoadContext,
        PluginDescriptor Descriptor,
        IPluginModule Module);
}
