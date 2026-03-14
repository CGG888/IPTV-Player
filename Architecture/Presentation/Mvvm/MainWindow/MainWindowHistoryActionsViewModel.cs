using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;
using LibmpvIptvClient.Services;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow
{
    public class MainWindowHistoryActionsViewModel : ViewModelBase
    {
        public List<HistoryItem> LoadHistory(UserDataStore store)
        {
            return store.GetHistory().ToList();
        }

        public List<HistoryItem> DeleteSelected(UserDataStore store, System.Collections.IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return store.GetHistory().ToList();

            var keys = new List<string>();
            foreach (var item in selectedItems)
            {
                if (item is HistoryItem hi && !string.IsNullOrWhiteSpace(hi.Key))
                {
                    keys.Add(hi.Key);
                }
            }

            foreach (var k in keys)
            {
                store.RemoveHistory(k);
            }

            return store.GetHistory().ToList();
        }

        public List<HistoryItem> DeleteOne(UserDataStore store, HistoryItem item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Key))
            {
                store.RemoveHistory(item.Key);
            }
            return store.GetHistory().ToList();
        }

        public List<HistoryItem> ClearAll(UserDataStore store)
        {
            store.ClearHistory();
            return store.GetHistory().ToList();
        }

        public void HandleDoubleClick(
            HistoryItem hi,
            List<Channel> channels,
            Action<Channel> playChannelAction,
            Action<string, string> playUrlAction)
        {
            if (hi == null) return;

            // 1. Check if it's a non-live type (Catchup/Timeshift/Playback) -> Prefer URL
            string liveLabel = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
            bool isLive = string.Equals(hi.PlayTypeLabel, "直播", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(hi.PlayTypeLabel, liveLabel, StringComparison.OrdinalIgnoreCase);

            if (!isLive && !string.IsNullOrWhiteSpace(hi.SourceUrl))
            {
                playUrlAction(hi.SourceUrl, hi.PlayTypeLabel);
                return;
            }

            // 2. Live: Try to match Channel
            var ch = channels.FirstOrDefault(c => string.Equals(UserDataStore.ComputeKey(c), hi.Key, StringComparison.OrdinalIgnoreCase))
                     ?? channels.FirstOrDefault(c => string.Equals(c.Name ?? "", hi.Name ?? "", StringComparison.OrdinalIgnoreCase));

            if (ch != null)
            {
                playChannelAction(ch);
                return;
            }

            // 3. Fallback: Play URL directly if channel not found
            if (!string.IsNullOrWhiteSpace(hi.SourceUrl))
            {
                playUrlAction(hi.SourceUrl, "live");
            }
        }

        public void AddOrUpdateHistory(
            UserDataStore store,
            double position,
            double duration,
            Channel? currentChannel,
            string currentUrl,
            bool timeshiftActive,
            object? currentPlayingProgram, // Use object to avoid dep on EpgProgram if not imported, or add using
            string untitledLabel)
        {
            if (currentChannel == null && string.IsNullOrWhiteSpace(currentUrl)) return;

            string playType = "live";
            if (timeshiftActive) playType = "timeshift";
            else if (currentPlayingProgram != null) playType = "catchup";

            var key = currentChannel != null ? UserDataStore.ComputeKey(currentChannel, currentUrl) : (currentUrl ?? "");
            var name = currentChannel?.Name ?? untitledLabel;
            var logo = currentChannel?.Logo ?? "";
            var group = currentChannel?.Group ?? "";
            
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(currentUrl)) return;

            store.AddOrUpdateHistory(new HistoryItem
            {
                Key = key,
                Name = name,
                Logo = logo,
                Group = group,
                SourceUrl = currentUrl,
                PlayType = playType,
                PositionSec = position,
                DurationSec = duration
            });
        }
    }
}
