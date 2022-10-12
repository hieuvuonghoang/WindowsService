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
        public PortalServices PortalServices
        {
            get; set;
        }
        public Portals Portals { get; set; }
    }

    public class Portals
    {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string HttpClientName { get; set; }
        public string HttpClientBaseUri { get; set; }
    }

    public class PortalServices
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
