using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace SELService
{
    public partial class SELService : ServiceBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AppConfigs _appConfigs;
        private Timer _timer = null;

        public SELService()
        {
            InitializeComponent();
            try
            {
                _appConfigs = ConfigurationManager.GetSection("AppConfigs") as AppConfigs;
                if (_appConfigs == null)
                {
                    throw new ConfigurationErrorsException("AppConfigs are not defined");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
            }
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info($"OnStart: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
            var worker = new Worker(_appConfigs);
            _timer = new Timer(worker.Run, null, TimeSpan.Zero, TimeSpan.FromSeconds(_appConfigs.ServiceConfigs.RefreshTime));
        }

        protected override void OnStop()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, 0);
            }
            _logger.Info($"OnStop: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
        }

    }
}
