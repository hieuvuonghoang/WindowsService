using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet.CusLoggingProvider
{
    public class NLogLogger : ILogger
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string _categoryName;

        public NLogLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var logRecord = string.Format("{0}|{1}{2}", _categoryName, formatter(state, exception), exception != null ? "\n" + exception.StackTrace : "");
            switch (logLevel)
            {
                case LogLevel.Trace:
                    _logger.Trace(logRecord);
                    break;
                case LogLevel.Debug:
                    _logger.Debug(logRecord);
                    break;
                case LogLevel.Information:
                    _logger.Info(logRecord);
                    break;
                case LogLevel.Warning:
                    _logger.Warn(logRecord);
                    break;
                case LogLevel.Error:
                    _logger.Error(logRecord);
                    break;
                case LogLevel.Critical:
                    _logger.Fatal(logRecord);
                    break;
            }
            
        }
    }
}
