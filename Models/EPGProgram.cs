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
        public bool IsBooked { get; set; } // Already scheduled reminder

        public string Status 
        { 
            get 
            {
                var now = DateTime.Now;
                if (now >= Start && now < End) return LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
                if (now >= End) return LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                return LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Scheduled", "预约");
            }
        }
    }
}
