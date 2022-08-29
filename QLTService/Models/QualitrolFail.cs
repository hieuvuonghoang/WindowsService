using System;
using System.Collections.Generic;
using System.Text;

namespace QLTService
{
    public class QualitrolFail
    {
        public long ResultID { get; set; }
        public string CircuitName { get; set; }
        public decimal ResultTimeStampLocal { get; set; }
        public decimal ResultTimeStampUS { get; set; }
        public string DeviceNameX { get; set; }
        public string DeviceNameY { get; set; }
        public double DTFX { get; set; }
        public double DTFY { get; set; }
        public DateTime DateTimeLocal
        {
            get
            {
                var a = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
                a = a.AddSeconds(Decimal.ToDouble(ResultTimeStampLocal));
                return a;
            }
        }
    }
}
