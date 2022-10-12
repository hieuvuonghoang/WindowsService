using Autofac;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace AutoRemoveCuSet
{
    public partial class WinService : ServiceBase
    {
        private readonly ILogger<WinService> _logger = Config.Container.Resolve<ILogger<WinService>>();
        private Timer _timer = null;

        public WinService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogInformation($"OnStart: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void DoWork(object state)
        {
            try
            {
                _logger.LogInformation("OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), ex);
            }
        }

        protected override void OnStop()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, 0);
            }
            _logger.LogInformation($"OnStop: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
        }

    }
}
