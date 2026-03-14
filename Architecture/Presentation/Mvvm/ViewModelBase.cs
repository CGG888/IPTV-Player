namespace LibmpvIptvClient.Architecture.Presentation.Mvvm;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string _title = string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }
}
