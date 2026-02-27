using System.Threading.Tasks;

namespace LibmpvIptvClient.Playback
{
    public interface IPlaybackController
    {
        Task Play(string url);
        Task Stop();
    }
}
