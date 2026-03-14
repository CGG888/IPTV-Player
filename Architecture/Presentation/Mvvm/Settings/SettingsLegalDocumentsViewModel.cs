using LibmpvIptvClient.Architecture.Application.Settings;
using LibmpvIptvClient.Architecture.Application.Shared;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record SettingsLegalDocumentOpenResult(
    bool IsSuccess,
    string Title,
    string Content,
    string ErrorMessage);

public sealed class SettingsLegalDocumentsViewModel(
    ISettingsLegalDocumentService legalService,
    ILocalizationService localizationService) : ViewModelBase
{
    public async Task<SettingsLegalDocumentOpenResult> LoadLicenseAsync(CancellationToken cancellationToken = default)
    {
        var title = localizationService.GetText("UI_License", "开源许可证");
        var error = localizationService.GetText("Msg_FetchLicenseFailed", "无法获取许可证内容。");
        var content = await legalService.LoadLicenseContentAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SettingsLegalDocumentOpenResult(false, title, string.Empty, error);
        }

        return new SettingsLegalDocumentOpenResult(true, title, content, string.Empty);
    }

    public async Task<SettingsLegalDocumentOpenResult> LoadThirdPartyAsync(CancellationToken cancellationToken = default)
    {
        var title = localizationService.GetText("UI_ThirdParty", "第三方声明");
        var error = localizationService.GetText("Msg_FetchThirdPartyFailed", "无法获取第三方声明内容。");
        var content = await legalService.LoadThirdPartyContentAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SettingsLegalDocumentOpenResult(false, title, string.Empty, error);
        }

        return new SettingsLegalDocumentOpenResult(true, title, content, string.Empty);
    }
}
