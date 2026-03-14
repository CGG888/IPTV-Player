using System.Text.Json;
using LibmpvIptvClient.Architecture.Plugin;
using System.IO;

namespace LibmpvIptvClient.Architecture.Tooling;

public sealed class PluginTemplateGenerator
{
    public async Task<string> GenerateAsync(string pluginsRoot, string pluginId, CancellationToken cancellationToken = default)
    {
        var pluginDirectory = Path.Combine(pluginsRoot, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var manifest = new PluginManifest(
            pluginId,
            $"{pluginId} Plugin",
            $"{pluginId}.EntryPlugin",
            $"{pluginId}.dll",
            "1.0.0.0",
            "1.1.0.0",
            Array.Empty<string>(),
            Array.Empty<string>());

        var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
        await using var stream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, cancellationToken: cancellationToken).ConfigureAwait(false);
        return pluginDirectory;
    }
}
