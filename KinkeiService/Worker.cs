using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KinkeiService
{
    public class Worker
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AppConfigs _appConfigs;

        public Worker(AppConfigs appConfigs)
        {
            _appConfigs = appConfigs;
        }

        public async void RunAsync(object state)
        {
            try
            {
                var lines = await GetLinesAsync();
                var devices = await GetDevicesAsync();
                var root = await GetFLResultAsync();
                var events = MapRootToEventView(root, lines, devices);
                var maxId = ReadMaxId();
                var nRowInPage = _appConfigs.PageConfigs.MaxRowInPage;
                events = events
                    .Where(it => it.Id > maxId)
                    .ToList();
                if (events.Count == 0)
                {
                    return;
                }
                var maxIdInResult = events.Max(it => it.Id);
                var nPage = events.Count / nRowInPage;
                if (events.Count % nRowInPage != 0)
                {
                    nPage++;
                }
                var eventView2ss = new List<List<EventView>>();
                for (var i = 1; i <= nPage; i++)
                {
                    eventView2ss.Add(events.Skip((i - 1) * nRowInPage).Take(nRowInPage).ToList());
                }
                if (eventView2ss.Count != 0)
                {
                    foreach (var eventView2s in eventView2ss)
                    {
                        var requestContent = new RequestContent()
                        {
                            APIKey = _appConfigs.WebServiceConfigs.APIKey,
                            MaDVQL = _appConfigs.DonViConfigs.MaDVQL,
                            TenDVQL = _appConfigs.DonViConfigs.TenDVQL,
                            EventView2s = eventView2s,
                        };
                        SendDatas(requestContent);
                        var mesLog = new StringBuilder();
                        foreach (var t in eventView2s)
                        {
                            mesLog.AppendLine(string.Format(" -- ID: {0}, StartTime: {1:dd/MM/yyyy HH:mm:ss.fff}, StationName: {2}, LineName: {3}, Length: {4}, StattionNameB: {5}, LengthB: {6}", t.Id, t.StartTime, t.StationName, t.LineName, t.Length, t.StationNameB, t.LengthB));
                        }
                        _logger.Info($"   + Sended ({eventView2s.Count()}):\n{mesLog}");
                    }
                    WriteMaxId(maxIdInResult);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
                throw ex;
            }
        }

        private async Task<LineResultFL> GetLinesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    var request = new HttpRequestMessage(HttpMethod.Get, _appConfigs.KinkeiConfigs.Lines);
                    var response = await client.SendAsync(request);
                    if(response.IsSuccessStatusCode)
                    {
                        var root = JsonConvert.DeserializeObject<LineResultFL>(await response.Content.ReadAsStringAsync());
                        return root;
                    } else
                    {
                        throw new Exception($"Lỗi khi gọi tới API: {_appConfigs.KinkeiConfigs.Lines}.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<DeviceResult> GetDevicesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    var request = new HttpRequestMessage(HttpMethod.Get, _appConfigs.KinkeiConfigs.Devices);
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var root = JsonConvert.DeserializeObject<DeviceResult>(await response.Content.ReadAsStringAsync());
                        return root;
                    }
                    else
                    {
                        throw new Exception($"Lỗi khi gọi tới API: {_appConfigs.KinkeiConfigs.Devices}.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<Root> GetFLResultAsync()
        {
            try
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                        var request = new HttpRequestMessage(HttpMethod.Get, _appConfigs.KinkeiConfigs.Results);
                        var response = await client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var root = JsonConvert.DeserializeObject<Root>(await response.Content.ReadAsStringAsync());
                            return root;
                        }
                        else
                        {
                            throw new Exception($"Lỗi khi gọi tới API: {_appConfigs.KinkeiConfigs.Results}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private List<EventView> MapRootToEventView(Root root, LineResultFL lines, DeviceResult devices)
        {
            var rets = new List<EventView>();
            try
            {
                foreach (var t in root.fl_results)
                {
                    if (t.fault_location.Count < 2)
                    {
                        var eventView = new EventView();
                        eventView.Id = t.id;
                        eventView.LineName = t.line_name;
                        eventView.Target = t.fault_phase;
                        eventView.StartTime = t.trigger_time;
                        rets.Add(eventView);
                        continue;
                    }
                    else
                    {
                        var eventView = new EventView();
                        eventView.Id = t.id;
                        eventView.LineName = t.line_name;
                        eventView.Target = t.fault_phase;
                        eventView.StartTime = t.trigger_time;
                        if (t.fault_location[0].distance_km <= t.fault_location[1].distance_km)
                        {
                            eventView.Length = t.fault_location[0].distance_km;
                            eventView.StationName = t.fault_location[0].from;
                            eventView.LengthB = t.fault_location[1].distance_km;
                            eventView.StationNameB = t.fault_location[1].from;
                        }
                        else
                        {
                            eventView.Length = t.fault_location[1].distance_km;
                            eventView.StationName = t.fault_location[1].from;
                            eventView.LengthB = t.fault_location[0].distance_km;
                            eventView.StationNameB = t.fault_location[0].from;
                        }
                        var line = lines.lines
                            .Where(it => it.line_name == eventView.LineName)
                            .FirstOrDefault();
                        if (line != null)
                        {
                            var device = line.terminals
                                .Where(it => it.substatio_name == eventView.StationName)
                                .FirstOrDefault();
                            if (device != null)
                            {
                                var deviceName = devices.devices
                                    .Where(it => it.device_no == device.device_no)
                                    .FirstOrDefault();
                                if (deviceName != null)
                                {
                                    eventView.DeviceName = deviceName.device_name;
                                }
                            }
                        }
                        rets.Add(eventView);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return rets;
        }

        /// <summary>
        /// Read max id in file
        /// </summary>
        /// <returns></returns>
        private int ReadMaxId()
        {
            var maxId = 0;
            try
            {
                var dir = _appConfigs.FileConfigs.Dir;
                var fileName = _appConfigs.FileConfigs.FileName;
                var strMaxIdFileTempPath = Path.Combine(Path.GetTempPath(), dir, fileName);
                var str = "0";
                if (File.Exists(strMaxIdFileTempPath))
                {
                    str = File.ReadAllText(strMaxIdFileTempPath);
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
                throw ex;
            }
            return maxId;
        }

        /// <summary>
        /// Write max id to file
        /// </summary>
        /// <param name="maxId"></param>
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
                throw ex;
            }
        }

        /// <summary>
        /// Gửi dữ liệu tới WebServices
        /// </summary>
        /// <param name="requestContent">Dữ liệu gửi đi</param>
        /// <returns></returns>
        private void SendDatas(RequestContent requestContent)
        {
            var content = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _appConfigs.WebServiceConfigs.URI)
            {
                Content = content
            };
            using (var client = new HttpClient())
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                Task.Run(() =>
                {
                    client.SendAsync(request);
                });
            }
        }
    }
}
