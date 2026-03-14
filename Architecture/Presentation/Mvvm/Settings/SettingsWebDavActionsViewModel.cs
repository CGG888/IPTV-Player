using LibmpvIptvClient.Architecture.Application.Shared;
using LibmpvIptvClient.Services;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record WebDavActionResult(
    string Title,
    string Message,
    bool ShouldReload,
    bool IsCancelled);

public sealed record WebDavRestoreConfirmation(
    bool ShouldConfirm,
    string Title,
    string Message);

public sealed class SettingsWebDavActionsViewModel(
    SettingsWebDavViewModel webDavViewModel,
    ILocalizationService localizationService) : ViewModelBase
{
    public async Task<WebDavActionResult> TestConnectionAsync(SettingsWebDavFormState form, CancellationToken cancellationToken = default)
    {
        var title = localizationService.GetText("Common_Tips", "提示");
        var cfg = webDavViewModel.BuildTestConfig(form);
        var validation = webDavViewModel.ValidateAndNormalizeBaseUrl(cfg.BaseUrl);
        if (!validation.IsValid)
        {
            return new WebDavActionResult(title, validation.ErrorMessage, false, false);
        }

        cfg.BaseUrl = validation.NormalizedBaseUrl;
        var cli = new WebDavClient(cfg);
        var ok = await cli.TestConnectionAsync(cfg.BaseUrl).ConfigureAwait(false);
        var msg = ok
            ? localizationService.GetText("Msg_WebDAV_Connected", "连接成功")
            : localizationService.GetText("Msg_WebDAV_ConnectionFailed", "连接失败");
        return new WebDavActionResult(title, msg, false, false);
    }

    public async Task<WebDavActionResult> BackupUserDataAsync(SettingsWebDavFormState form, string baseDir, CancellationToken cancellationToken = default)
    {
        var title = localizationService.GetText("Common_Tips", "提示");
        var cfg = webDavViewModel.BuildUserDataTransferConfig(form);
        var result = await webDavViewModel.BackupUserDataAsync(cfg, baseDir, cancellationToken).ConfigureAwait(false);
        return new WebDavActionResult(title, result.Message, false, false);
    }

    public WebDavRestoreConfirmation BuildRestoreConfirmation(string baseDir)
    {
        var title = localizationService.GetText("Common_Tips", "提示");
        var message = localizationService.GetText("UI_WebDAV_Restore_Confirm", "将覆盖本地用户数据，确定继续？");
        return new WebDavRestoreConfirmation(webDavViewModel.HasLocalUserData(baseDir), title, message);
    }

    public async Task<WebDavActionResult> RestoreUserDataAsync(SettingsWebDavFormState form, string baseDir, CancellationToken cancellationToken = default)
    {
        var title = localizationService.GetText("Common_Tips", "提示");
        var cfg = webDavViewModel.BuildUserDataTransferConfig(form);
        var result = await webDavViewModel.RestoreUserDataAsync(cfg, baseDir, cancellationToken).ConfigureAwait(false);
        return new WebDavActionResult(title, result.Message, result.SuccessCount > 0, false);
    }

    public async Task<WebDavActionResult> TestRecordingsWriteAsync(SettingsWebDavFormState form, CancellationToken cancellationToken = default)
    {
        var title = localizationService.GetText("Common_Tips", "提示");
        var cfg = webDavViewModel.BuildRecordingsTransferConfig(form);
        var result = await webDavViewModel.TestRecordingsWriteAsync(cfg, cancellationToken).ConfigureAwait(false);
        return new WebDavActionResult(title, result.Message, false, false);
    }
}
