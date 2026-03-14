namespace LibmpvIptvClient.Architecture.Plugin;

public sealed record PluginManifest(
    string Id,
    string Name,
    string EntryType,
    string AssemblyFile,
    string Version,
    string MinHostVersion,
    string[] Dependencies,
    string[] ViewIds);
