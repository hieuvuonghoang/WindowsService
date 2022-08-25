using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SELService
{
    public class Worker
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AppConfigs _appConfigs;
        private readonly DbProviderFactory _factory;
        private readonly DbConnection _connection;

        private readonly string _querySQLGetDataForPage;
        private readonly string _querySQLGetMaxTriggerTimeInResult;
        private readonly string _querySQLGetNRowInResult;

        public Worker(AppConfigs appConfigs)
        {
            _appConfigs = appConfigs;

            var unitTest = ConfigurationManager.ConnectionStrings["UnitTest"];

            if(unitTest != null)
            {
                _factory = DbProviderFactories.GetFactory(unitTest.ProviderName);

                if (_factory != null)
                {
                    _connection = _factory.CreateConnection();
                    _connection.ConnectionString = unitTest.ConnectionString;
                }
                else
                {
                    throw new InvalidOperationException("Failed creating connection.");
                }
            } else
            {
                throw new InvalidOperationException("Failed load ConnectionStrings.");
            }

            _logger.Info(_connection.Database);

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

        public async void RunAsync(object state)
        {
            try
            {
                //var maxId = ReadMaxId();
                //_logger.Info($"{maxId}");
                //maxId++;
                //WriteMaxId(maxId);
                await TestAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
                throw ex;
            }
        }

        private async Task TestAsync()
        {
            try
            {
                await _connection.OpenAsync();
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM API_Log";
                using (var result = await command.ExecuteReaderAsync())
                {
                    while (result.Read())
                    {
                        if (!result.IsDBNull(0))
                        {
                            var ret = result.GetInt32(0);
                            _logger.Info(ret);
                        }
                    }
                }
            } catch (Exception ex)
            {
                _logger.Error(ex.ToString(), ex);
            }
        }

        ///// <summary>
        ///// Get maxID
        ///// </summary>
        ///// <param name="maxID">maxID hiện tại</param>
        ///// <returns></returns>
        //private async Task<int> GetMaxIdInResult(int maxId)
        //{
        //    int ret = 0;
        //    //using (var command = _aTSFL_SELContext.Database.GetDbConnection().CreateCommand())
        //    //{
        //    //    command.CommandText = string.Format(_querySQLGetMaxTriggerTimeInResult, maxId);
        //    //    await _aTSFL_SELContext.Database.OpenConnectionAsync();
        //    //    using (var result = await command.ExecuteReaderAsync())
        //    //    {
        //    //        while (result.Read())
        //    //        {
        //    //            if (!result.IsDBNull(0))
        //    //            {
        //    //                ret = result.GetInt32(0);
        //    //            }
        //    //        }
        //    //    }
        //    //}
        //    return ret;
        //}

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
                //_logger.Error(ex.ToString(), ex);
                throw ex;
            }
        }
    }
}
