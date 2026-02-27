using System;

namespace LibmpvIptvClient.Models
{
    public class EpgProgram
    {
        public string Title { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Description { get; set; } = "";
        
        public bool IsPlaying { get; set; } // New property

        public string Status 
        { 
            get 
            {
                var now = DateTime.Now;
                if (now >= Start && now < End) return "直播";
                if (now >= End) return "回放";
                return "预约";
            }
        }
    }
}