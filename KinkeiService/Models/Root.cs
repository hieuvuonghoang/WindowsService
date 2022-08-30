using System;
using System.Collections.Generic;
using System.Text;

namespace KinkeiService
{
    public class FaultLocation
    {
        public string from { get; set; }
        public double distance_km { get; set; }
    }

    public class FaultSection
    {
        public string from { get; set; }
        public string to { get; set; }
    }

    public class FlResult
    {
        public int id { get; set; }
        public DateTime trigger_time { get; set; }
        public bool gps_sync { get; set; }
        public bool simulation { get; set; }
        public string line_no { get; set; }
        public string line_name { get; set; }
        public List<FaultSection> fault_section { get; set; }
        public string fault_phase { get; set; }
        public string computing_method { get; set; }
        public List<FaultLocation> fault_location { get; set; }
        public int? segment_number { get; set; }
        public double? segment_distance_km { get; set; }
        public double? segment_length_km { get; set; }
    }

    public class Root
    {
        public List<FlResult> fl_results { get; set; }
    }


}
