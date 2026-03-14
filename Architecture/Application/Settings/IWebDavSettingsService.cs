using LibmpvIptvClient.Architecture.Application.Shared;

namespace LibmpvIptvClient.Architecture.Application.Settings;

public sealed record WebDavTransferResult(
    int SuccessCount,
    string Message);

public interface IWebDavSettingsService
{
    /// <summary>
    /// Checks if local user data files exist.
    /// </summary>
    bool HasLocalUserData(string baseDir);

    /// <summary>
    /// Backs up user data files (user_settings.json, user_data.json) to WebDAV.
    /// </summary>
    Task<WebDavTransferResult> BackupUserDataAsync(WebDavConfig config, string localBaseDir, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores user data files from WebDAV to the local directory.
    /// </summary>
    Task<WebDavTransferResult> RestoreUserDataAsync(WebDavConfig config, string localBaseDir, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests write permissions by creating a test file in the recordings directory.
    /// </summary>
    Task<WebDavTransferResult> TestRecordingsWriteAsync(WebDavConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and normalizes the WebDAV base URL.
    /// </summary>
    (bool IsValid, string NormalizedUrl, string ErrorMessage) ValidateAndNormalizeBaseUrl(string? baseUrl);
}
