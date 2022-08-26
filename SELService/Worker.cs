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

namespace SELService
{
    public class Worker
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AppConfigs _appConfigs;
        private readonly string _connectionString;
        private readonly string _querySQLGetDataForPage;
        private readonly string _querySQLGetMaxTriggerTimeInResult;
        private readonly string _querySQLGetNRowInResult;

        public Worker(AppConfigs appConfigs)
        {
            _appConfigs = appConfigs;

            var unitTest = ConfigurationManager.ConnectionStrings["ATS_DB"];

            if (unitTest != null && !string.IsNullOrEmpty(unitTest.ConnectionString))
            {
                _connectionString = unitTest.ConnectionString;
            }
            else
            {
                throw new ConfigurationErrorsException("Vui lòng kiểm tra lại cấu hình chuỗi kết nối tới Database.");
            }

            _querySQLGetDataForPage =
                        @"SELECT 
                        a.Name 
                        ,e.TriggerTime 
                        ,e.Id 
                        ,e.Distance_1 
                        ,e.Distance_2 
                        ,e.LineId 
                        ,f.Name as Line1Name 
                        ,g.Name as Line2Name 
                        ,e.Target
                        ,d.Name as DeviceName
                        ,i.StationName as TramA1
                        ,j.StationName as TramB1
                        ,k.StationName as TramA2
                        ,l.StationName as TramB2
                        FROM [ATS_FL].[dbo].[Stations] a 
                        INNER JOIN [ATS_FL].[dbo].[Yards] b ON a.Id = b.StationId 
                        INNER JOIN [ATS_FL].[dbo].[Bays] c ON b.Id = c.YardId 
                        INNER JOIN [ATS_FL].[dbo].[Devices] d ON c.Id = d.BayId 
                        INNER JOIN [ATS_FL].[dbo].[FaultResults] e ON d.Id = e.DeviceId 
                        LEFT JOIN [ATS_FL].[dbo].[Lines] f ON d.Id = f.Device_1 
                        LEFT JOIN [ATS_FL].[dbo].[Lines] g ON d.Id = g.Device_2 
                        LEFT JOIN [ATS_FL].[dbo].[Devices] h ON h.Name = d.Name
                        LEFT JOIN [ATS_FL].[dbo].StationDevicesView i ON i.Id = f.Device_1
                        LEFT JOIN [ATS_FL].[dbo].StationDevicesView j ON j.Id = f.Device_2
                        LEFT JOIN [ATS_FL].[dbo].StationDevicesView k ON k.Id = g.Device_1
                        LEFT JOIN [ATS_FL].[dbo].StationDevicesView l ON l.Id = g.Device_2
                        WHERE h.KinkeyStationName IS NULL AND (e.Distance_1 > 0 OR e.Target LIKE 'INST%') AND e.Id > {0} AND e.Id <= {1} 
                        ORDER BY e.TriggerTime 
                        OFFSET {2} ROWS 
                        FETCH FIRST {3} ROWS ONLY";

            _querySQLGetMaxTriggerTimeInResult =
                        @"SELECT 
                        MAX(e.Id) 
                        FROM [ATS_FL].[dbo].[Stations] a 
                        INNER JOIN [ATS_FL].[dbo].[Yards] b ON a.Id = b.StationId 
                        INNER JOIN [ATS_FL].[dbo].[Bays] c ON b.Id = c.YardId 
                        INNER JOIN [ATS_FL].[dbo].[Devices] d ON c.Id = d.BayId 
                        INNER JOIN [ATS_FL].[dbo].[FaultResults] e ON d.Id = e.DeviceId 
                        LEFT JOIN [ATS_FL].[dbo].[Lines] f ON d.Id = f.Device_1 
                        LEFT JOIN [ATS_FL].[dbo].[Lines] g ON d.Id = g.Device_2 
                        LEFT JOIN [ATS_FL].[dbo].[Devices] h ON h.Name = d.Name 
                        WHERE h.KinkeyStationName IS NULL AND (e.Distance_1 > 0 OR e.Target LIKE 'INST%') AND e.Id > {0}";

            _querySQLGetNRowInResult =
                        @"SELECT 
                        COUNT(e.Id) 
                        FROM [ATS_FL].[dbo].[Stations] a 
                        INNER JOIN [ATS_FL].[dbo].[Yards] b ON a.Id = b.StationId 
                        INNER JOIN [ATS_FL].[dbo].[Bays] c ON b.Id = c.YardId 
                        INNER JOIN [ATS_FL].[dbo].[Devices] d ON c.Id = d.BayId 
                        INNER JOIN [ATS_FL].[dbo].[FaultResults] e ON d.Id = e.DeviceId 
                        LEFT JOIN [ATS_FL].[dbo].[Lines] f ON d.Id = f.Device_1 
                        LEFT JOIN [ATS_FL].[dbo].[Lines] g ON d.Id = g.Device_2 
                        LEFT JOIN [ATS_FL].[dbo].[Devices] h ON h.Name = d.Name 
                        WHERE h.KinkeyStationName IS NULL AND (e.Distance_1 > 0 OR e.Target LIKE 'INST%') AND e.Id > {0} AND e.Id <= {1}";
        }

        public void Run(object state)
        {
            try
            {
                var maxId = ReadMaxId();
                var nRowInPage = _appConfigs.PageConfigs.MaxRowInPage;
                int maxIDInResult = 0;
                using(var connection = new SqlConnection(_connectionString))
                {
                    maxIDInResult = GetMaxIdInResult(maxId, connection);
                    if (maxIDInResult != 0)
                    {
                        var nRowInResult = GetNRowInResult(maxId, maxIDInResult, connection);
                        var nPage = nRowInResult / nRowInPage;
                        if (nRowInResult % nRowInPage != 0)
                        {
                            nPage++;
                        }
                        var eventView2ss = new List<List<EventView>>();
                        for (var i = 1; i <= nPage; i++)
                        {
                            eventView2ss.Add(GetDataForPage(maxId, maxIDInResult, i, nRowInPage, connection));
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
                                    mesLog.AppendLine(string.Format(
                                        " -- ID: {0}, StartTime: {1:dd/MM/yyyy HH:mm:ss.fff}, StationName: {2}, " +
                                        "LineName: {3}, Length: {4}, StationNameB: {5}, LengthB: {6}", 
                                        t.Id, t.StartTime, t.StationName, t.LineName, t.Length, t.StationNameB, t.LengthB));
                                }
                                _logger.Info($"   + Sended ({eventView2s.Count()}):\n{mesLog}");
                            }
                            WriteMaxId(maxIDInResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
                throw ex;
            }
        }

        /// <summary>
        /// Get maxID in result
        /// </summary>
        /// <param name="maxID">maxID hiện tại</param>
        /// <returns></returns>
        private int GetMaxIdInResult(int maxId, SqlConnection connection)
        {
            var ret = 0;
            try
            {
                using (var command = new SqlCommand(string.Format(_querySQLGetMaxTriggerTimeInResult, maxId), connection))
                {
                    command.Connection.Open();
                    using (var result = command.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            if (!result.IsDBNull(0))
                            {
                                ret = result.GetInt32(0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ret;
        }

        /// <summary>
        /// Get tổng số bản ghi trong nữa đoạn (maxID, maxIDInResult]
        /// </summary>
        /// <param name="maxId">maxID hiện tại</param>
        /// <param name="maxIdInResult">maxID trong kết quả truy vấn</param>
        /// <returns></returns>
        private int GetNRowInResult(int maxId, int maxIdInResult, SqlConnection connection)
        {
            var ret = 0;
            using (var command = new SqlCommand(string.Format(_querySQLGetNRowInResult, maxId, maxIdInResult), connection))
            {
                command.Connection.Open();
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        if (!result.IsDBNull(0))
                        {
                            ret = result.GetInt32(0);
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Get data cho từng trang
        /// </summary>
        /// <param name="maxID">maxID hiện tại</param>
        /// <param name="maxIDInResult">maxID trong kết quả truy vấn</param>
        /// <param name="pageNum">trang hiện tại</param>
        /// <param name="nRowInPage">số bản ghi trên một trang</param>
        /// <returns></returns>
        private List<EventView> GetDataForPage(int maxId, int maxIdInResult, int pageNum, int nRowInPage, SqlConnection connection)
        {
            var rets = new List<EventView>();
            try
            {

                var nSkip = (pageNum - 1) * nRowInPage;
                var nTake = nRowInPage;
                using (var command = new SqlCommand(string.Format(_querySQLGetDataForPage, maxId, maxIdInResult, nSkip, nTake), connection))
                {
                    command.Connection.Open();
                    using (var result = command.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            var ret = new EventView();
                            ret.LineName = !result.IsDBNull(6) ? result.GetString(6) : !result.IsDBNull(7) ? result.GetString(7) : null;
                            if (!string.IsNullOrEmpty(ret.LineName))
                            {
                                ret.LineName = ret.LineName.Replace(System.Environment.NewLine, "");
                            }
                            ret.StationName = result.GetString(0).Replace(System.Environment.NewLine, "");
                            ret.DeviceName = result.GetString(9).Replace(System.Environment.NewLine, "");

                            ret.StartTime = result.GetDateTime(1);
                            ret.Id = result.GetInt32(2);
                            if (!result.IsDBNull(8))
                            {
                                ret.Target = result.GetString(8);
                            }
                            if (!result.IsDBNull(5))
                            {
                                //LineId IS NOT NULL GET Distance_2
                                if (!result.IsDBNull(4))
                                {
                                    ret.Length = result.GetDouble(4);
                                }
                                if (!result.IsDBNull(3))
                                {
                                    ret.LengthB = result.GetDouble(3);
                                }
                            }
                            else
                            {
                                //LineId IS NULL GET Distance_1
                                if (!result.IsDBNull(3))
                                {
                                    ret.Length = result.GetDouble(3);
                                }
                                if (!result.IsDBNull(4))
                                {
                                    ret.LengthB = result.GetDouble(4);
                                }
                            }

                            if (!result.IsDBNull(6))
                            {
                                if (!result.IsDBNull(10) && ret.StationName != result.GetString(10))
                                {
                                    ret.StationNameB = result.GetString(10);
                                }
                                if (!result.IsDBNull(11) && ret.StationName != result.GetString(11))
                                {
                                    ret.StationNameB = result.GetString(11);
                                }
                            }
                            else
                            {
                                if (!result.IsDBNull(12) && ret.StationName != result.GetString(12))
                                {
                                    ret.StationNameB = result.GetString(12);
                                }
                                if (!result.IsDBNull(13) && ret.StationName != result.GetString(13))
                                {
                                    ret.StationNameB = result.GetString(13);
                                }
                            }

                            rets.Add(ret);
                        }
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
