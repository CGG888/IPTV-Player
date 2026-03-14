using LibmpvIptvClient.Architecture.Platform;
using LibmpvIptvClient.Architecture.Presentation.Html;

namespace LibmpvIptvClient.Architecture.Core;

public interface IPluginModule : IAsyncDisposable
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken);
    ValueTask StartAsync(CancellationToken cancellationToken);
    ValueTask StopAsync(CancellationToken cancellationToken);
}

public sealed record PluginContext(
    ServiceRegistry Services,
    IPlatformAdapter PlatformAdapter,
    IHtmlCssUiEngine UiEngine,
    RuntimePlatform RuntimePlatform);
