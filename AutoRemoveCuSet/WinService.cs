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
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AppConfigs _appConfigs;
        private Timer _timer = null;

        public WinService()
        {
            InitializeComponent();
            try
            {
                _appConfigs = ConfigurationManager.GetSection("AppConfigs") as AutoRemoveCuSet.AppConfigs;
                if (_appConfigs == null)
                {
                    throw new Exception("App Config are not defined");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
                throw;
            }
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info($"OnStart: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void DoWork(object state)
        {
            try
            {
                _logger.Info("OK");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
            }
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
