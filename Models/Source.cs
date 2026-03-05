namespace LibmpvIptvClient.Models
{
    public enum StreamProtocol
    {
        HLS,
        DASH,
        RTSP,
        RTP,
        SRT,
        HTTP,
        FILE
    }
    public enum TransportHint
    {
        Auto,
        Tcp,
        Udp,
        UdpMulticast,
        Http
    }
    public class SourceQuality
    {
        public int Height { get; set; }
        public int Bitrate { get; set; }
        public string Codec { get; set; } = "";
        public double Fps { get; set; }
    }
    public class SourceFcc
    {
        public bool Supported { get; set; }
        public string? BurstTemplate { get; set; }
    }
    public class Source
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = ""; // 存储源的描述名称（如“组播高清”）
        public string ChannelId { get; set; } = "";
        public string Url { get; set; } = "";
        public StreamProtocol Protocol { get; set; }
        public TransportHint Transport { get; set; } = TransportHint.Auto;
        public SourceQuality Quality { get; set; } = new SourceQuality();
        public string Region { get; set; } = "";
        public int Priority { get; set; }
        public SourceFcc? Fcc { get; set; }
    }
}
