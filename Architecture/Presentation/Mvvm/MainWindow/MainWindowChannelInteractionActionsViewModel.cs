using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed class MainWindowChannelInteractionActionsViewModel : ViewModelBase
{
    public Channel? ResolveDoubleClickChannel(MouseButtonEventArgs e, object? sender)
    {
        if (e == null || e.ClickCount != 2)
        {
            return null;
        }

        if (sender is FrameworkElement fe && fe.DataContext is Channel ch)
        {
            return ch;
        }

        return null;
    }

    public bool ToggleFavorite(Channel channel, Func<Channel, string> computeKey, Action<string, bool> persistFavorite)
    {
        if (channel == null || computeKey == null || persistFavorite == null)
        {
            return false;
        }

        var next = !channel.Favorite;
        channel.Favorite = next;
        var key = computeKey(channel);
        persistFavorite(key, next);
        return true;
    }

    public IReadOnlyList<Channel> BuildFavoriteList(IReadOnlyList<Channel> channels, Func<Channel, string> computeKey)
    {
        var list = new List<Channel>();
        if (channels == null || computeKey == null)
        {
            return list;
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in channels)
        {
            if (c?.Favorite != true) continue;
            var key = computeKey(c);
            if (set.Add(key)) list.Add(c);
        }
        return list;
    }
}
