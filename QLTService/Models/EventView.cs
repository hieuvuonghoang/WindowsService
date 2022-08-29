using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLTService
{
    public class EventView
    {
        /// <summary>
        /// ID sự cố
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Thời gian xảy ra sự cố
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// Đường dây xảy ra sự cố
        /// </summary>
        public string LineName { get; set; }
        /// <summary>
        /// Trạm nhận thông tin sự cố
        /// </summary>
        public string StationName { get; set; }
        /// <summary>
        /// Khoảng cách sự cố đến trạm nhận thông tin sự cố
        /// </summary>
        public double Length { get; set; }
        /// <summary>
        /// Pha sự cố
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// Tên thiết bị báo sự cố
        /// </summary>
        public string DeviceName { get; set; }
        /// <summary>
        /// Trạm B
        /// </summary>
        public string StationNameB { get; set; }
        /// <summary>
        /// Khoảng cách từ trạm B
        /// </summary>
        public double LengthB { get; set; }
    }
}
