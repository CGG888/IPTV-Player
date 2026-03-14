namespace LibmpvIptvClient.Architecture.Presentation.Html;

public sealed record ResponsiveBreakpoint(string Name, int MinWidth);
public sealed record AnimationDefinition(string Name, int DurationMs, string TimingFunction);

public sealed class HtmlCssRuntimeFeatures
{
    private readonly List<ResponsiveBreakpoint> _breakpoints =
    [
        new ResponsiveBreakpoint("mobile", 0),
        new ResponsiveBreakpoint("tablet", 768),
        new ResponsiveBreakpoint("desktop", 1200)
    ];

    private readonly Dictionary<string, AnimationDefinition> _animations = new(StringComparer.OrdinalIgnoreCase);

    public HtmlCssRuntimeFeatures()
    {
        _animations["fade-in"] = new AnimationDefinition("fade-in", 220, "ease-out");
        _animations["slide-up"] = new AnimationDefinition("slide-up", 280, "cubic-bezier(0.2, 0, 0, 1)");
    }

    public ResponsiveBreakpoint ResolveBreakpoint(int width)
    {
        return _breakpoints
            .Where(x => width >= x.MinWidth)
            .OrderByDescending(x => x.MinWidth)
            .First();
    }

    public bool TryGetAnimation(string name, out AnimationDefinition? animation)
    {
        if (_animations.TryGetValue(name, out var found))
        {
            animation = found;
            return true;
        }

        animation = null;
        return false;
    }
}
