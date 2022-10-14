using Autofac;
using AutoRemoveCuSet.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ILogger<WinService> _logger;
        private readonly AppConfigs _appConfigs;
        private readonly IPortalServices _portalServices;
        private Timer _schedular;

        public WinService(
            ILogger<WinService> logger,
            IOptions<AppConfigs> appConfigs,
            IPortalServices portalServices)
        {
            InitializeComponent();
            _logger = logger;
            _appConfigs = appConfigs.Value;
            _portalServices = portalServices;
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogInformation($"OnStart: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
            ScheduleService();
        }

        protected override void OnStop()
        {
            _schedular.Dispose();
            _logger.LogInformation($"OnStop: {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")}");
        }

        public void ScheduleService()
        {
            try
            {
                _schedular = new Timer(new TimerCallback(SchedularCallbackAsync));

                // Get the Scheduled Time from AppConfigs.
                var now = DateTime.Now;

                var scheduledTime = new DateTime(
                    now.Year, 
                    now.Month, 
                    now.Day, 
                    _appConfigs.Hours, 
                    _appConfigs.Minutes, 
                    _appConfigs.Seconds);

                if (now > scheduledTime)
                {
                    // If Scheduled Time is passed set Schedule for the next day.
                    scheduledTime = scheduledTime.AddDays(1);
                }

                var timeSpan = scheduledTime.Subtract(now);

                // Get the difference in Minutes between the Scheduled and Current Time.
                var dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                // Change the Timer's Due Time.
                _schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }
        }

        private async void SchedularCallbackAsync(object e)
        {
            await DoWorkAsync();
            this.ScheduleService();
        }

        private async Task DoWorkAsync()
        {
            try
            {
                var now = DateTime.Now.AddHours(_appConfigs.AddHours);

                var accessToken = await _portalServices.GeneratePortalTokeAsync();

                // Xóa dữ liệu cú sét 1 ngày.
                try
                {
                    await _portalServices.RemoveFeatureCuSet(Models.CuSetType.CuSet1Ngay, now, accessToken);
                    _logger.LogInformation("Xóa dữ liệu cú sét 1 ngày thành công!");
                } catch(Exception ex)
                {
                    _logger.LogError("Lỗi xảy ra khi xóa dữ liệu cú sét 1 ngày. " + ex.Message, ex);
                }

                // Xóa dữ liệu cú sét 1 tháng (30 ngày).
                try
                {
                    await _portalServices.RemoveFeatureCuSet(Models.CuSetType.CuSet1Thang, now.AddDays(-30), accessToken);
                    _logger.LogInformation("Xóa dữ liệu cú sét 30 ngày thành công!");
                } catch(Exception ex)
                {
                    _logger.LogError("Lỗi xảy ra khi xóa dữ liệu cú sét 30 ngày. " + ex.Message, ex);
                }

                // Xóa dữ liệu cú sét 1 quý (90 ngày).
                try
                {
                    await _portalServices.RemoveFeatureCuSet(Models.CuSetType.CuSet1Quy, now.AddDays(-90), accessToken);
                    _logger.LogInformation("Xóa dữ liệu cú sét 90 ngày thành công!");
                } catch(Exception ex)
                {
                    _logger.LogError("Lỗi xảy ra khi xóa dữ liệu cú sét 90 ngày. " + ex.Message, ex);
                }

            } catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

    }
}
