using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet.Services
{
    public class PortalServices : IPortalServices
    {
        private readonly ILogger<PortalServices> _logger;

        public PortalServices(ILogger<PortalServices> logger)
        {
            _logger = logger;
        }

        public void TestLogger()
        {
            for(var i = 0; i < 100; i++)
            {
                Task.Run(() =>
                {
                    _logger.LogInformation("HieuVH");
                });
            }
            
        }
    }
}
