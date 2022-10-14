using AutoRemoveCuSet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AutoRemoveCuSet.Services
{
    public class PortalServices : IPortalServices
    {
        private readonly ILogger<PortalServices> _logger;
        private readonly AppConfigs _appConfigs;
        private readonly IHttpClientFactory _httpClientFactory;

        public PortalServices(
            ILogger<PortalServices> logger, 
            IOptions<AppConfigs> appConfigs,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _appConfigs = appConfigs.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PortalResultToken> GeneratePortalTokeAsync()
        {
            PortalResultToken ret = null;
            try
            {
                var inforService = _appConfigs.ServicePortals;
                var inforPortal = _appConfigs.Portals;
                var formdata = new MultipartFormDataContent();
                formdata.Add(new StringContent(inforPortal.UserName), "username");
                formdata.Add(new StringContent(inforPortal.PassWord), "password");
                formdata.Add(new StringContent("referer"), "client");
                formdata.Add(new StringContent(inforPortal.RootPath), "referer");
                formdata.Add(new StringContent($"{inforPortal.ExpiredToken}"), "expiration");
                formdata.Add(new StringContent("json"), "f");
                var client = _httpClientFactory.CreateClient(inforPortal.HttpClientName);
                var response = await client.PostAsync(inforService.GenerateToken, formdata);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var jObject = JObject.Parse(responseBody);
                if(jObject["error"] != null)
                {
                    throw new Exception(jObject["error"]["message"].ToString());
                }
                ret = JsonConvert.DeserializeObject<PortalResultToken>(responseBody);
            } catch(Exception)
            {
                throw;
            }
            return ret;
        }

        public async Task<bool> RemoveFeatureCuSet(CuSetType cuSetType, DateTime thoiGianCS, PortalResultToken accessToken)
        {
            var ret = false;
            try
            {
                var whereSQL = $"THOIGIAN_CS  <= TIMESTAMP '{thoiGianCS.ToString("yyyy-MM-dd HH:mm:ss")}'";
                _logger.LogInformation($" WHERE: {whereSQL}");
                var inforService = _appConfigs.ServicePortals;
                var inforPortal = _appConfigs.Portals;
                var formdata = new MultipartFormDataContent();
                formdata.Add(new StringContent(whereSQL), "where");
                formdata.Add(new StringContent("json"), "f");
                formdata.Add(new StringContent("false"), "rollbackOnFailure");
                formdata.Add(new StringContent("false"), "returnDeleteResults");

                var requestUri = "";
                switch(cuSetType)
                {
                    case CuSetType.CuSet1Ngay:
                        requestUri = inforService.CuSet1Ngay;
                        break;
                    case CuSetType.CuSet1Thang:
                        requestUri = inforService.CuSet1Thang;
                        break;
                    case CuSetType.CuSet1Quy:
                        requestUri = inforService.CuSet1Quy;
                        break;
                }

                if(string.IsNullOrEmpty(requestUri))
                {
                    throw new Exception("Lỗi requestUri IsNullOrEmpty?");
                }

                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Headers.Add("Authorization", $"Bearer {accessToken.token}");
                request.Content = formdata;
                
                var client = _httpClientFactory.CreateClient(inforPortal.HttpClientName);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var jObject = JObject.Parse(responseBody);
                if (jObject["error"] != null)
                {
                    throw new Exception(jObject["error"]["message"].ToString());
                }
                ret = true;
            } catch(Exception)
            {
                throw;
            }
            return ret;
        }

    }
}
