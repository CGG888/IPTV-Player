using System.Net.Http;
using System.IO;

namespace LibmpvIptvClient.Architecture.Application.Settings;

public interface ISettingsLegalDocumentService
{
    Task<string?> LoadLicenseContentAsync(CancellationToken cancellationToken = default);
    Task<string?> LoadThirdPartyContentAsync(CancellationToken cancellationToken = default);
}

public sealed class SettingsLegalDocumentService(HttpClient httpClient) : ISettingsLegalDocumentService
{
    public async Task<string?> LoadLicenseContentAsync(CancellationToken cancellationToken = default)
    {
        var content = LoadLocalContent("LICENSE.txt", "license.txt");
        if (!string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        return await LoadRemoteContentAsync("https://raw.githubusercontent.com/CGG888/SrcBox/main/LICENSE.txt", cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> LoadThirdPartyContentAsync(CancellationToken cancellationToken = default)
    {
        var content = LoadLocalContent("THIRD-PARTY-NOTICES.txt", "Third-Party-Notices.txt");
        if (!string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        return await LoadRemoteContentAsync("https://raw.githubusercontent.com/CGG888/SrcBox/main/THIRD-PARTY-NOTICES.txt", cancellationToken).ConfigureAwait(false);
    }

    private static string? LoadLocalContent(params string[] fileNames)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var fileName in fileNames)
            {
                var path = Path.Combine(baseDir, fileName);
                if (!File.Exists(path))
                {
                    continue;
                }

                var content = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private async Task<string?> LoadRemoteContentAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(8));
            return await httpClient.GetStringAsync(url, cts.Token).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
