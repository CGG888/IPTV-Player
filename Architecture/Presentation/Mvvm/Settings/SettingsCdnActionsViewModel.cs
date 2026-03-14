namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsCdnActionsViewModel : ViewModelBase
{
    private readonly SettingsCdnViewModel _cdnViewModel;

    public SettingsCdnActionsViewModel(SettingsCdnViewModel cdnViewModel)
    {
        _cdnViewModel = cdnViewModel;
    }

    public string? AddFromInput(string? rawInput)
    {
        var text = (rawInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        _cdnViewModel.AddCdn(text);
        return string.Empty;
    }

    public void RemoveSelected(System.Collections.IList? selectedItems)
    {
        _cdnViewModel.RemoveCdn(selectedItems);
    }

    public void MoveUp(object? selectedItem)
    {
        _cdnViewModel.MoveUp(selectedItem as string);
    }

    public void MoveDown(object? selectedItem)
    {
        _cdnViewModel.MoveDown(selectedItem as string);
    }

    public async Task RunSpeedTestAsync()
    {
        await _cdnViewModel.RunSpeedTestAsync().ConfigureAwait(true);
    }
}
