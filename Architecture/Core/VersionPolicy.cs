namespace LibmpvIptvClient.Architecture.Core;

public interface IVersionPolicy
{
    bool IsCompatible(Version hostVersion, Version pluginVersion);
}

public sealed class MajorMinorVersionPolicy : IVersionPolicy
{
    public bool IsCompatible(Version hostVersion, Version pluginVersion)
    {
        return hostVersion.Major == pluginVersion.Major && hostVersion.Minor >= pluginVersion.Minor;
    }
}
