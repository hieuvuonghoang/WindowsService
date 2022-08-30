using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinkeiService
{
    public class RequestContent
    {
        /// <summary>
        /// API Key dùng để xác thực với WebServices
        /// </summary>
        public string APIKey { get; set; }
        /// <summary>
        /// Mã đơn vị quản lý
        /// </summary>
        public string MaDVQL { get; set; }
        /// <summary>
        /// Tên đơn vị quản lý
        /// </summary>
        public string TenDVQL { get; set; }
        /// <summary>
        /// Dữ liệu sự cố SEL
        /// </summary>
        public List<EventView> EventView2s { get; set; }
    }
}
