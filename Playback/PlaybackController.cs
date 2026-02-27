using System.Threading.Tasks;

namespace LibmpvIptvClient.Playback
{
    public class PlaybackController : IPlaybackController
    {
        readonly MpvInterop _mpv;
        public PlaybackController(MpvInterop mpv)
        {
            _mpv = mpv;
        }
        public Task Play(string url)
        {
            _mpv.LoadFile(url);
            return Task.CompletedTask;
        }
        public Task Stop()
        {
            _mpv.Stop();
            return Task.CompletedTask;
        }
    }
}
