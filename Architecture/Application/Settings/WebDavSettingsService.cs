using LibmpvIptvClient.Architecture.Application.Shared;
using LibmpvIptvClient.Services;
using System.IO;

namespace LibmpvIptvClient.Architecture.Application.Settings;

public sealed class WebDavSettingsService(ILocalizationService localizationService) : IWebDavSettingsService
{
    public (bool IsValid, string NormalizedUrl, string ErrorMessage) ValidateAndNormalizeBaseUrl(string? baseUrl)
    {
        var raw = (baseUrl ?? string.Empty).Trim();
        if (!Uri.TryCreate(raw, UriKind.Absolute, out _))
        {
            return (
                false,
                string.Empty,
                localizationService.GetText("UI_WebDAV_BaseUrl_Invalid", "请填写完整的 WebDAV 基地址，例如 https://example.com/remote.php/dav/files/<user>/"));
        }

        if (!raw.EndsWith("/", StringComparison.Ordinal))
        {
            raw += "/";
        }

        return (true, raw, string.Empty);
    }

    public bool HasLocalUserData(string baseDir)
    {
        var settingsPath = Path.Combine(baseDir, "user_settings.json");
        var userDataPath = Path.Combine(baseDir, "user_data.json");
        return File.Exists(settingsPath) || File.Exists(userDataPath);
    }

    public async Task<WebDavTransferResult> BackupUserDataAsync(WebDavConfig config, string baseDir, CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, normalizedUrl, errorMessage) = ValidateAndNormalizeBaseUrl(config.BaseUrl);
            if (!isValid)
            {
                return new WebDavTransferResult(0, errorMessage);
            }

            config.BaseUrl = normalizedUrl;
            var cli = new WebDavClient(config);
            await cli.EnsureCollectionAsync(config.UserDataPath).ConfigureAwait(false);
            
            var count = 0;
            var settingsPath = Path.Combine(baseDir, "user_settings.json");
            var userDataPath = Path.Combine(baseDir, "user_data.json");
            
            if (File.Exists(settingsPath))
            {
                var bytes = await File.ReadAllBytesAsync(settingsPath, cancellationToken).ConfigureAwait(false);
                var ok = await cli.PutAsync(cli.Combine(config.UserDataPath + "/user_settings.json"), bytes, "application/json").ConfigureAwait(false);
                if (ok) count++;
            }

            if (File.Exists(userDataPath))
            {
                var bytes = await File.ReadAllBytesAsync(userDataPath, cancellationToken).ConfigureAwait(false);
                var ok = await cli.PutAsync(cli.Combine(config.UserDataPath + "/user_data.json"), bytes, "application/json").ConfigureAwait(false);
                if (ok) count++;
            }

            var message = count > 0 ? "备份完成" : "备份失败";
            return new WebDavTransferResult(count, message);
        }
        catch
        {
            return new WebDavTransferResult(0, "备份失败");
        }
    }

    public async Task<WebDavTransferResult> RestoreUserDataAsync(WebDavConfig config, string baseDir, CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, normalizedUrl, errorMessage) = ValidateAndNormalizeBaseUrl(config.BaseUrl);
            if (!isValid)
            {
                return new WebDavTransferResult(0, errorMessage);
            }

            config.BaseUrl = normalizedUrl;
            var cli = new WebDavClient(config);
            var count = 0;
            var settingsPath = Path.Combine(baseDir, "user_settings.json");
            var userDataPath = Path.Combine(baseDir, "user_data.json");
            
            var r1 = await cli.GetBytesAsync(cli.Combine(config.UserDataPath + "/user_settings.json")).ConfigureAwait(false);
            if (r1.ok && r1.bytes.Length > 0)
            {
                await File.WriteAllBytesAsync(settingsPath, r1.bytes, cancellationToken).ConfigureAwait(false);
                count++;
            }

            var r2 = await cli.GetBytesAsync(cli.Combine(config.UserDataPath + "/user_data.json")).ConfigureAwait(false);
            if (r2.ok && r2.bytes.Length > 0)
            {
                await File.WriteAllBytesAsync(userDataPath, r2.bytes, cancellationToken).ConfigureAwait(false);
                count++;
            }

            var message = count > 0
                ? localizationService.GetText("Msg_WebDAV_RestoreDone", "恢复完成")
                : localizationService.GetText("Msg_WebDAV_RestoreFailed", "恢复失败");
            return new WebDavTransferResult(count, message);
        }
        catch
        {
            return new WebDavTransferResult(0, localizationService.GetText("Msg_WebDAV_RestoreFailed", "恢复失败"));
        }
    }

    public async Task<WebDavTransferResult> TestRecordingsWriteAsync(WebDavConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, normalizedUrl, errorMessage) = ValidateAndNormalizeBaseUrl(config.BaseUrl);
            if (!isValid)
            {
                return new WebDavTransferResult(0, errorMessage);
            }

            config.BaseUrl = normalizedUrl;
            var cli = new WebDavClient(config);
            await cli.EnsureCollectionAsync(config.RecordingsPath).ConfigureAwait(false);
            
            var content = System.Text.Encoding.UTF8.GetBytes($"SrcBox write test {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            var ok = await cli.PutAsync(cli.Combine(config.RecordingsPath + "/write_test.txt"), content, "text/plain").ConfigureAwait(false);
            
            var message = ok
                ? localizationService.GetText("Msg_WebDAV_RecordingsWriteDone", "录播目录写入成功")
                : localizationService.GetText("Msg_WebDAV_RecordingsWriteFailed", "录播目录写入失败");
            return new WebDavTransferResult(ok ? 1 : 0, message);
        }
        catch
        {
            return new WebDavTransferResult(0, localizationService.GetText("Msg_WebDAV_RecordingsWriteFailed", "录播目录写入失败"));
        }
    }
}
