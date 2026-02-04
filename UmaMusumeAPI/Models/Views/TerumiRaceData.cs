using System.Collections.Generic;

namespace UmaMusumeAPI.Models.Views
{
    public class TerumiRaceData
    {
        public int RaceId { get; set; }
        public string RaceName { get; set; }
        public int Grade { get; set; }
        public string GradeName { get; set; }
        public int Distance { get; set; }
        public string DistanceCategory { get; set; } // Short, Mile, Middle, Long
        public int Ground { get; set; }
        public string GroundName { get; set; } // Turf, Dirt
        public int Turn { get; set; }
        public string TurnName { get; set; } // Left, Right, Straight
        public int TrackId { get; set; }
        public string TrackName { get; set; }
        public int EntryNum { get; set; }
        public List<RaceSchedule> Schedules { get; set; }
    }

    public class RaceSchedule
    {
        public int InstanceId { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Half { get; set; } // 1 = first half, 2 = second half
        public string HalfName { get; set; }
        public int Time { get; set; }
        public string TimeName { get; set; } // Morning, Afternoon, Evening
    }
}
