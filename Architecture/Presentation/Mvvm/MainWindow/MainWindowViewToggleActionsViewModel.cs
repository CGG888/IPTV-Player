using System;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record EpgRefreshPlan(bool ShouldRefresh, DateTime? PreferredTime);

public sealed class MainWindowViewToggleActionsViewModel : ViewModelBase
{
    public bool ResolveFullscreenTarget(bool? isChecked)
    {
        return isChecked == true;
    }

    public bool ResolveDrawerCollapsed(bool? isChecked)
    {
        return isChecked != true;
    }

    public bool ResolveEpgVisible(bool? isChecked)
    {
        return isChecked == true;
    }

    public EpgRefreshPlan BuildEpgRefreshPlan(bool show, bool hasChannel, bool strictMatchByPlaybackTime, bool hasPlaybackContext, DateTime? playbackTime)
    {
        if (!show || !hasChannel)
        {
            return new EpgRefreshPlan(false, null);
        }

        if (strictMatchByPlaybackTime && hasPlaybackContext && playbackTime.HasValue)
        {
            return new EpgRefreshPlan(true, playbackTime.Value);
        }

        return new EpgRefreshPlan(true, null);
    }
}
