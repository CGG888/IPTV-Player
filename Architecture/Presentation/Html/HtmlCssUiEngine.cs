using System.Diagnostics;
using System.Text.RegularExpressions;
using LibmpvIptvClient.Architecture.Core;

namespace LibmpvIptvClient.Architecture.Presentation.Html;

public sealed class HtmlCssUiEngine : IHtmlCssUiEngine
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{([a-zA-Z0-9\._-]+)\}\}", RegexOptions.Compiled);
    private readonly Dictionary<string, HtmlCssViewDefinition> _views = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterView(HtmlCssViewDefinition view)
    {
        _views[view.Id] = view;
    }

    public bool TryGetView(string viewId, out HtmlCssViewDefinition? view)
    {
        if (_views.TryGetValue(viewId, out var found))
        {
            view = found;
            return true;
        }

        view = null;
        return false;
    }

    public HtmlCssRenderResult Render(HtmlCssRenderRequest request)
    {
        var start = Stopwatch.GetTimestamp();
        var html = BindData(request.View.Html, request.DataContext);
        var css = request.View.Css;
        var markup = $"<style>{css}</style>{html}";
        var duration = Stopwatch.GetElapsedTime(start);
        var isAccelerated = request.Platform is RuntimePlatform.Windows or RuntimePlatform.MacOS or RuntimePlatform.Android or RuntimePlatform.IOS;
        return new HtmlCssRenderResult(markup, duration, isAccelerated);
    }

    private static string BindData(string html, IReadOnlyDictionary<string, object?> dataContext)
    {
        return PlaceholderRegex.Replace(html, match =>
        {
            var key = match.Groups[1].Value;
            if (dataContext.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }

            return string.Empty;
        });
    }
}
