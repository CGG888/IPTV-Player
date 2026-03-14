using LibmpvIptvClient.Architecture.Core;

namespace LibmpvIptvClient.Architecture.Presentation.Html;

public sealed record HtmlCssViewDefinition(
    string Id,
    string Html,
    string Css,
    string? JavaScript = null);

public sealed record HtmlCssRenderRequest(
    HtmlCssViewDefinition View,
    IReadOnlyDictionary<string, object?> DataContext,
    RuntimePlatform Platform);

public sealed record HtmlCssRenderResult(
    string Markup,
    TimeSpan RenderDuration,
    bool IsHardwareAccelerated);

public interface IHtmlCssUiEngine
{
    HtmlCssRenderResult Render(HtmlCssRenderRequest request);
    void RegisterView(HtmlCssViewDefinition view);
    bool TryGetView(string viewId, out HtmlCssViewDefinition? view);
}
