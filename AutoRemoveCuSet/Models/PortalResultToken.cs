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
    }
}
