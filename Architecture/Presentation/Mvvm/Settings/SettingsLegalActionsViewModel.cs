using LibmpvIptvClient.Architecture.Application.Shared;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record LegalDocumentActionState(
    object? ButtonContent,
    bool IsButtonEnabled,
    bool ShouldShowDialog,
    string DialogTitle,
    string DialogContent,
    string ErrorMessage,
    string TipsTitle);

public sealed class SettingsLegalActionsViewModel(
    SettingsLegalDocumentsViewModel legalDocumentsViewModel,
    ILocalizationService localizationService) : ViewModelBase
{
    public LegalDocumentActionState CreateLoadingState(object? buttonContent)
    {
        return new LegalDocumentActionState(
            buttonContent,
            false,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            localizationService.GetText("Common_Tips", "提示"));
    }

    public LegalDocumentActionState CreateErrorState(string errorMessage)
    {
        return new LegalDocumentActionState(
            null,
            true,
            false,
            string.Empty,
            string.Empty,
            errorMessage,
            localizationService.GetText("Common_Tips", "提示"));
    }

    public LegalDocumentActionState CreateDialogState(string title, string content)
    {
        return new LegalDocumentActionState(
            null,
            true,
            true,
            title,
            content,
            string.Empty,
            localizationService.GetText("Common_Tips", "提示"));
    }

    public async Task<LegalDocumentActionState> LoadLicenseAsync(CancellationToken cancellationToken = default)
    {
        var result = await legalDocumentsViewModel.LoadLicenseAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return CreateErrorState(result.ErrorMessage);
        }

        return CreateDialogState(result.Title, result.Content);
    }

    public async Task<LegalDocumentActionState> LoadThirdPartyAsync(CancellationToken cancellationToken = default)
    {
        var result = await legalDocumentsViewModel.LoadThirdPartyAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return CreateErrorState(result.ErrorMessage);
        }

        return CreateDialogState(result.Title, result.Content);
    }
}
