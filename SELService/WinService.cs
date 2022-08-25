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
                _appConfigs = ConfigurationManager.GetSection("AppConfigs") as global::SELService.AppConfigs;
                if (_appConfigs == null)
                {
                    throw new Exception("App Config are not defined");
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
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void DoWork(object state)
        {
            try
            {
                var maxId = ReadMaxId();
                _logger.Info($"{maxId}");
                maxId++;
                WriteMaxId(maxId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
                throw ex;
            }
        }

        private int ReadMaxId()
        {
            var maxId = 0;
            try
            {
                var dir = _appConfigs.FileConfigs.Dir;
                var fileName = _appConfigs.FileConfigs.FileName;
                var strMaxIdFileTempPath = Path.Combine(Path.GetTempPath(), dir, fileName);
                var str = "";
                if (File.Exists(strMaxIdFileTempPath))
                {
                    str = File.ReadAllText(strMaxIdFileTempPath);
                }
                else
                {
                    var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var strWorkPath = Path.GetDirectoryName(strExeFilePath);
                    var strMaxIdFilePath = Path.Combine(strWorkPath, fileName);
                    str = File.ReadAllText(strMaxIdFilePath);
                }
                try
                {
                    maxId = int.Parse(str);
                }
                catch (FormatException ex)
                {
                    maxId = 0;
                    _logger.Warn(ex.ToString(), ex);
                }
            }
            catch (Exception ex)
            {
                //_logger.Error(ex.ToString(), ex);
                throw ex;
            }
            return maxId;
        }

        private void WriteMaxId(int maxId)
        {
            try
            {
                var dir = _appConfigs.FileConfigs.Dir;
                var fileName = _appConfigs.FileConfigs.FileName;
                var strMaxIdFileTempPath = Path.Combine(Path.GetTempPath(), dir, fileName);
                var pathDir = Path.Combine(Path.GetTempPath(), dir);
                if (!Directory.Exists(pathDir))
                {
                    Directory.CreateDirectory(pathDir);
                }
                File.WriteAllText(strMaxIdFileTempPath, $"{maxId}");
            }
            catch (Exception ex)
            {
                //_logger.Error(ex.ToString(), ex);
                throw ex;
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
