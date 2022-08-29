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

namespace QLTService
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

            var unitTest = ConfigurationManager.ConnectionStrings["QLT_DB"];

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
	                    FLResults.ResultID, 
	                    FLCircuits.CircuitName, 
	                    FLResults.ResultTimeStampLocal, 
	                    FLResults.ResultTimeStampUS, 
	                    c.DeviceName as DeviceNameX, 
	                    d.DeviceName as DeviceNameY, 
	                    FLResults.DTFX, 
	                    FLResults.DTFY 
                        FROM FLResults 
                        INNER JOIN FLCircuits ON FLResults.CircuitID = FLCircuits.CircuitId 
                        LEFT JOIN Feeders a ON FLResults.FeederX = a.FeederId 
                        LEFT JOIN Feeders b ON FLResults.FeederY = b.FeederId 
                        LEFT JOIN Device c ON a.DeviceId = c.DeviceId 
                        LEFT JOIN Device d ON b.DeviceId = d.DeviceId 
                        WHERE FLResults.ResultID > {0} AND FLResults.ResultID <= {1} AND FLResults.DTFX > 0 AND FLResults.DTFY > 0 
                        ORDER BY FLResults.ResultID 
                        OFFSET {2} ROWS 
                        FETCH FIRST {3} ROWS ONLY ";

            _querySQLGetMaxTriggerTimeInResult =
                        @"SELECT 
	                    MAX(FLResults.ResultID) 
                        FROM FLResults 
                        INNER JOIN FLCircuits ON FLResults.CircuitID = FLCircuits.CircuitId 
                        LEFT JOIN Feeders a ON FLResults.FeederX = a.FeederId 
                        LEFT JOIN Feeders b ON FLResults.FeederY = b.FeederId 
                        LEFT JOIN Device c ON a.DeviceId = c.DeviceId 
                        LEFT JOIN Device d ON b.DeviceId = d.DeviceId 
                        WHERE FLResults.ResultID > {0} AND FLResults.DTFX > 0 AND FLResults.DTFY > 0 ";

            _querySQLGetNRowInResult =
                        @"SELECT 
	                    COUNT(FLResults.ResultID) 
                        FROM FLResults 
                        INNER JOIN FLCircuits ON FLResults.CircuitID = FLCircuits.CircuitId 
                        LEFT JOIN Feeders a ON FLResults.FeederX = a.FeederId 
                        LEFT JOIN Feeders b ON FLResults.FeederY = b.FeederId 
                        LEFT JOIN Device c ON a.DeviceId = c.DeviceId 
                        LEFT JOIN Device d ON b.DeviceId = d.DeviceId 
                        WHERE FLResults.ResultID > {0} AND FLResults.ResultID <= {1} AND FLResults.DTFX > 0 AND FLResults.DTFY > 0 ";
        }

        public void Run(object state)
        {
            try
            {
                var maxId = ReadMaxId();
                var nRowInPage = _appConfigs.PageConfigs.MaxRowInPage;
                long maxIDInResult = 0;
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
                            var offset = (i - 1) * nRowInPage;
                            var fetch = nRowInPage;
                            var eventView2s = GetDataForPage(maxId, maxIDInResult, offset, fetch, connection);
                            eventView2ss.Add(MapFLResultToSuCoAttributes(eventView2s));
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
        /// Mapping dữ liệu FLResult sang SuCoAttribute.
        /// </summary>
        /// <param name="fLResults"></param>
        /// <returns></returns>
        private List<EventView> MapFLResultToSuCoAttributes(List<QualitrolFail> qualitrolFails)
        {
            var rets = new List<EventView>();
            try
            {
                foreach (var qualitrolFail in qualitrolFails)
                {
                    var eventView2 = new EventView();
                    eventView2.Id = qualitrolFail.ResultID;
                    eventView2.StartTime = qualitrolFail.DateTimeLocal;
                    eventView2.LineName = qualitrolFail.CircuitName;
                    if (qualitrolFail.DTFX < qualitrolFail.DTFY)
                    {
                        eventView2.StationName = qualitrolFail.DeviceNameX;
                        eventView2.Length = qualitrolFail.DTFX;
                        eventView2.DeviceName = qualitrolFail.DeviceNameX;

                        eventView2.LengthB = qualitrolFail.DTFY;
                        eventView2.StationNameB = qualitrolFail.DeviceNameY;
                    }
                    else
                    {
                        eventView2.StationName = qualitrolFail.DeviceNameY;
                        eventView2.Length = qualitrolFail.DTFY;
                        eventView2.DeviceName = qualitrolFail.DeviceNameY;

                        eventView2.LengthB = qualitrolFail.DTFX;
                        eventView2.StationNameB = qualitrolFail.DeviceNameX;
                    }
                    rets.Add(eventView2);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return rets;
        }

        /// <summary>
        /// Get maxID in result
        /// </summary>
        /// <param name="maxID">maxID hiện tại</param>
        /// <returns></returns>
        private long GetMaxIdInResult(long maxId, SqlConnection connection)
        {
            long ret = 0;
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
                                ret = result.GetInt64(0);
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
        private int GetNRowInResult(long maxId, long maxIdInResult, SqlConnection connection)
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
        private List<QualitrolFail> GetDataForPage(long maxId, long maxIdInResult, int pageNum, int nRowInPage, SqlConnection connection)
        {
            var qualitrolFails = new List<QualitrolFail>();
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
                            var qualitrolFail = new QualitrolFail();
                            qualitrolFail.ResultID = result.GetInt64(0);
                            qualitrolFail.CircuitName = result.GetString(1).ToString();
                            qualitrolFail.ResultTimeStampLocal = result.GetDecimal(2);
                            qualitrolFail.ResultTimeStampUS = result.GetDecimal(3);
                            qualitrolFail.DeviceNameX = result.GetString(4).ToString();
                            qualitrolFail.DeviceNameY = result.GetString(5).ToString();
                            qualitrolFail.DTFX = result.GetDouble(6);
                            qualitrolFail.DTFY = result.GetDouble(7);
                            qualitrolFails.Add(qualitrolFail);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return qualitrolFails;
        }

        /// <summary>
        /// Read max id in file
        /// </summary>
        /// <returns></returns>
        private long ReadMaxId()
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
        private void WriteMaxId(long maxId)
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
