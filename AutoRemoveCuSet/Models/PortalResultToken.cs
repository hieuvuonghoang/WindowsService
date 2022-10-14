using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet.Models
{
    public class PortalResultToken
    {
        public string token { get; set; }
        public long expires { get; set; }
        public bool ssl { get; set; }
        /// <summary>
        /// - Token đã hết hạn?
        /// - Token được xem là hết hạn nếu: 
        ///    + Thời gian hiện tại +5 phút >= Thời gian hết hạn của token.
        /// </summary>
        public bool isExpired
        {
            get
            {
                bool ret = false;
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var dateTime = epoch.AddMilliseconds(expires).ToLocalTime();
                if(dateTime <= DateTime.Now.AddMinutes(5))
                {
                    ret = true;
                }
                return ret;
            }
        }
    }
}
