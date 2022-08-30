using System;
using System.Collections.Generic;
using System.Text;

namespace KinkeiService
{
    public class Device
    {
        public string device_no { get; set; }
        public string device_name { get; set; }
    }

    public class DeviceResult
    {
        public List<Device> devices { get; set; }
    }
}

