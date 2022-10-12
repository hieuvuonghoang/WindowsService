using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet.CusLoggingProvider
{
    public class NLogLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new NLogLogger(categoryName);
        }

        public void Dispose()
        {
            
        }
    }
}
