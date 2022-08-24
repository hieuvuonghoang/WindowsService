using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsService
{
    public partial class WinService : ServiceBase, IDisposable
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private Timer _timer = null;

        public WinService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info($"OnStart: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void DoWork(object state)
        {
            _logger.Info($"DoWork: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");

            try
            {

                var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var strWorkPath = Path.GetDirectoryName(strExeFilePath);
                var strMaxIdFilePath = Path.Combine(strWorkPath, "StoreMaxID.txt");

                _logger.Info(strMaxIdFilePath);

                var maxId = int.Parse(File.ReadAllText(strMaxIdFilePath));

                _logger.Info($"{maxId}");

                maxId++;

                File.WriteAllText(strMaxIdFilePath, $"{maxId}");
            } catch(Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
                throw ex;
            }
        }

        protected override void OnStop()
        {
            if(_timer != null)
            {
                _timer.Change(Timeout.Infinite, 0);
            }
            _logger.Info($"OnStop: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
        }

    }
}
