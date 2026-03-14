using LibmpvIptvClient.Architecture.Application.Settings;
using LibmpvIptvClient.Architecture.Application.Shared;
using System.IO;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record SettingsWebDavFormState(
    bool Enabled,
    string BaseUrl,
    string Username,
    string Token,
    bool AllowSelfSignedCert,
    string RootPath,
    string RecordingsPath,
    string UserDataPath);

public sealed record SettingsWebDavValidationResult(
    bool IsValid,
    string NormalizedBaseUrl,
    string ErrorMessage);

public sealed class SettingsWebDavViewModel(
    ILocalizationService localizationService,
    IWebDavSettingsService webDavService) : ViewModelBase
{
    public SettingsWebDavFormState BuildFormState(WebDavConfig? config)
    {
        if (config is null)
        {
            return new SettingsWebDavFormState(
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                "/srcbox/",
                "/srcbox/recordings/",
                "/srcbox/user-data/");
        }

        var token = LibmpvIptvClient.Services.CryptoUtil.UnprotectString(config.EncryptedToken ?? string.Empty);
        return new SettingsWebDavFormState(
            config.Enabled,
            config.BaseUrl ?? string.Empty,
            config.Username ?? string.Empty,
            token,
            config.AllowSelfSignedCert,
            config.RootPath ?? "/srcbox/",
            config.RecordingsPath ?? "/srcbox/recordings/",
            config.UserDataPath ?? "/srcbox/user-data/");
    }

    public WebDavConfig BuildSaveConfig(SettingsWebDavFormState form)
    {
        var token = (form.Token ?? string.Empty).Trim();
        return new WebDavConfig
        {
            Enabled = form.Enabled,
            BaseUrl = (form.BaseUrl ?? string.Empty).Trim(),
            Username = (form.Username ?? string.Empty).Trim(),
            TokenOrPassword = token,
            EncryptedToken = LibmpvIptvClient.Services.CryptoUtil.ProtectString(token),
            AllowSelfSignedCert = form.AllowSelfSignedCert,
            RootPath = NormalizePath(form.RootPath, "/srcbox/"),
            RecordingsPath = NormalizePath(form.RecordingsPath, "/srcbox/recordings/"),
            UserDataPath = NormalizePath(form.UserDataPath, "/srcbox/user-data/")
        };
    }

    public WebDavConfig BuildTestConfig(SettingsWebDavFormState form)
    {
        return new WebDavConfig
        {
            BaseUrl = (form.BaseUrl ?? string.Empty).Trim(),
            Username = (form.Username ?? string.Empty).Trim(),
            TokenOrPassword = (form.Token ?? string.Empty).Trim(),
            AllowSelfSignedCert = form.AllowSelfSignedCert
        };
    }

    public WebDavConfig BuildUserDataTransferConfig(SettingsWebDavFormState form)
    {
        var cfg = BuildTestConfig(form);
        cfg.UserDataPath = NormalizePath(form.UserDataPath, "/srcbox/user-data/");
        return cfg;
    }

    public WebDavConfig BuildRecordingsTransferConfig(SettingsWebDavFormState form)
    {
        var cfg = BuildTestConfig(form);
        cfg.RecordingsPath = NormalizePath(form.RecordingsPath, "/srcbox/recordings/");
        return cfg;
    }

    public SettingsWebDavValidationResult ValidateAndNormalizeBaseUrl(string? baseUrl)
    {
        var (isValid, normalized, error) = webDavService.ValidateAndNormalizeBaseUrl(baseUrl);
        return new SettingsWebDavValidationResult(isValid, normalized, error);
    }

    public bool HasLocalUserData(string baseDir)
    {
        return webDavService.HasLocalUserData(baseDir);
    }

    public async Task<WebDavTransferResult> BackupUserDataAsync(WebDavConfig cfg, string baseDir, CancellationToken cancellationToken = default)
    {
        return await webDavService.BackupUserDataAsync(cfg, baseDir, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WebDavTransferResult> RestoreUserDataAsync(WebDavConfig cfg, string baseDir, CancellationToken cancellationToken = default)
    {
        return await webDavService.RestoreUserDataAsync(cfg, baseDir, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WebDavTransferResult> TestRecordingsWriteAsync(WebDavConfig cfg, CancellationToken cancellationToken = default)
    {
        return await webDavService.TestRecordingsWriteAsync(cfg, cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizePath(string? path, string fallback)
    {
        var value = (path ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
