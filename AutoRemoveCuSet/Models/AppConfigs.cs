using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet
{
    public class AppConfigs
    {
        public ServicePortals ServicePortals
        {
            get; set;
        }
        public Portals Portals { get; set; }
        public string Time { get; set; }
        public int AddHours { get; set; }

        public int Hours
        {
            get
            {
                return Convert.ToInt32(Time.Split(':')[0]);
            }
        }
        public int Minutes
        {
            get
            {
                return Convert.ToInt32(Time.Split(':')[1]);
            }
        }
        public int Seconds
        {
            get
            {
                return Convert.ToInt32(Time.Split(':')[2]);
            }
        }
    }

    public class Portals
    {
        /// <summary>
        /// Tài khoản Login Portal.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string PassWord { get; set; }
        public string RootPath { get; set; }
        /// <summary>
        /// - Thời gian tồn tại của Access Token (Đơn vị Phút).
        /// - Yêu cầu lớn hơn 5 phút.
        /// </summary>
        public int ExpiredToken { get; set; }
        public string HttpClientName { get; set; }
        public string HttpClientBaseUri { get; set; }
    }

    public class ServicePortals
    {
        public string CuSet1Ngay
        {
            get; set;
        }
        public string CuSet1Thang
        {
            get; set;
        }
        public string CuSet1Quy
        {
            get; set;
        }
        public string GenerateToken
        {
            get; set;
        }
    }

}
