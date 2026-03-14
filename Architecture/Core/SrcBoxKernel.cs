using LibmpvIptvClient.Architecture.Platform;
using LibmpvIptvClient.Architecture.Plugin;
using LibmpvIptvClient.Architecture.Presentation.Html;
using LibmpvIptvClient.Architecture.Tooling;
using LibmpvIptvClient.Architecture.Application.Shared;
using LibmpvIptvClient.Architecture.Application.Settings;
using LibmpvIptvClient.Services;
using System.IO;

namespace LibmpvIptvClient.Architecture.Core;

public sealed class SrcBoxKernel : IAsyncDisposable
{
    private readonly ServiceRegistry _services = new();
    private IPluginManager? _pluginManager;

    public RuntimePlatform RuntimePlatform { get; private set; } = RuntimePlatform.Unknown;
    public IPlatformAdapter PlatformAdapter { get; private set; } = new GenericPlatformAdapter("Unknown");
    public IHtmlCssUiEngine UiEngine { get; private set; } = new HtmlCssUiEngine();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var adapterFactory = new PlatformAdapterFactory();
        RuntimePlatform = adapterFactory.DetectPlatform();
        PlatformAdapter = adapterFactory.CreateAdapter(RuntimePlatform);
        UiEngine = new HtmlCssUiEngine();

        _services.RegisterSingleton(adapterFactory);
        _services.RegisterSingleton<IPlatformAdapter>(PlatformAdapter);
        _services.RegisterSingleton(UiEngine);
        _services.RegisterSingleton<IVersionPolicy>(new MajorMinorVersionPolicy());
        _services.RegisterSingleton<ILocalizationService>(new ResourceLocalizationService());
        _services.RegisterSingleton<IThemeTitleBarService>(new WindowsTitleBarThemeService());
        _services.RegisterSingleton<ISettingsLegalDocumentService>(_ => new SettingsLegalDocumentService(HttpClientService.Instance.Client));
        _services.RegisterSingleton<IWebDavSettingsService>(_ => new WebDavSettingsService(_.Resolve<ILocalizationService>()));
        _services.RegisterSingleton<IUpdateService>(_ => new GithubUpdateService(HttpClientService.Instance.Client));
        _services.RegisterSingleton(new PluginTemplateGenerator());
        _services.RegisterSingleton(new PerformanceProfiler());
        _services.RegisterSingleton(new BuildPipelinePlanner());
        _services.RegisterSingleton(new ResourceReclaimer());

        var pluginsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        var versionPolicy = _services.Resolve<IVersionPolicy>();
        var hostVersion = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
        _pluginManager = new PluginManager(pluginsRoot, _services, PlatformAdapter, RuntimePlatform, UiEngine, versionPolicy, hostVersion);
        await _pluginManager.ScanAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LoadPluginOnDemandAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_pluginManager is null)
        {
            throw new InvalidOperationException("内核尚未初始化");
        }

        await _pluginManager.LoadAsync(pluginId, cancellationToken).ConfigureAwait(false);
    }

    public T Resolve<T>() where T : class => _services.Resolve<T>();

    public async ValueTask DisposeAsync()
    {
        if (_pluginManager is not null)
        {
            await _pluginManager.DisposeAsync().ConfigureAwait(false);
        }
    }
}
