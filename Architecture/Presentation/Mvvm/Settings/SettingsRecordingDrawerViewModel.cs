using LibmpvIptvClient.Controls;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsRecordingDrawerViewModel : ViewModelBase
{
    public RecordingConfig BuildTempConfig(RecordingConfig? source)
    {
        if (source == null)
        {
            return new RecordingConfig();
        }

        return new RecordingConfig
        {
            Enabled = source.Enabled,
            DefaultPlayChoice = source.DefaultPlayChoice,
            LastPlayChoice = source.LastPlayChoice,
            SaveMode = source.SaveMode,
            DirTemplate = source.DirTemplate,
            FileTemplate = source.FileTemplate,
            VerifyDirReady = source.VerifyDirReady,
            GrowthTimeoutSec = source.GrowthTimeoutSec,
            RetryCount = source.RetryCount,
            UploadMaxConcurrency = source.UploadMaxConcurrency,
            UploadRetry = source.UploadRetry,
            UploadRetryBackoffMs = source.UploadRetryBackoffMs,
            UploadMaxKBps = source.UploadMaxKBps,
            ResumeUpload = source.ResumeUpload,
            RealtimeUploadIntervalSec = source.RealtimeUploadIntervalSec,
            RemoteTempSuffix = source.RemoteTempSuffix,
            RealtimeFinalizeEnabled = source.RealtimeFinalizeEnabled,
            RealtimeFinalizeDelaySec = source.RealtimeFinalizeDelaySec,
            RealtimeFinalizeMaxKBps = source.RealtimeFinalizeMaxKBps
        };
    }

    public void LoadDrawer(RecordingDrawer? drawer, RecordingConfig config)
    {
        drawer?.Load(config);
    }

    public void SaveDrawer(RecordingDrawer? drawer, RecordingConfig config)
    {
        drawer?.Save(config);
    }
}
