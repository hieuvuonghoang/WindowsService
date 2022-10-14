using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoRemoveCuSet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Autofac.Core;
using AutoRemoveCuSet.Services;

namespace TestAutoRemoveCuSet
{
    [TestClass]
    public class TestAutofac
    {
        private IContainer _container;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .Build() as IConfiguration;

            var httpClientNamePortal = configuration.GetSection(nameof(AppConfigs)).GetSection(nameof(Portals)).GetSection("HttpClientName").Value;
            var httpClientBaseUriPortal = configuration.GetSection(nameof(AppConfigs)).GetSection(nameof(Portals)).GetSection("HttpClientBaseUri").Value;

            var _serviceCollection = new ServiceCollection();
            _serviceCollection.AddHttpClient(httpClientNamePortal, (client) =>
            {
                client.BaseAddress = new Uri(httpClientBaseUriPortal);
            });
            _serviceCollection.Configure<AppConfigs>(configuration.GetSection(nameof(AppConfigs)));
            _serviceCollection.AddLogging((loggingBuilder) =>
            {
            });

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(_serviceCollection);
            containerBuilder.RegisterType<PortalServices>().As<IPortalServices>().SingleInstance();

            _container = containerBuilder.Build();
        }

        [TestMethod]
        public void TestOptions()
        {
            var appConfig = _container.Resolve<IOptions<AppConfigs>>().Value;
            var expect = "server/rest/services/CuSet_1Ngay_IUD/FeatureServer/0/deleteFeatures";
            var actual = appConfig.ServicePortals.CuSet1Ngay;
            Assert.IsTrue(expect == actual);
        }

        [TestMethod]
        public void TestResolveHttpClient()
        {
            var appConfig = _container.Resolve<IOptions<AppConfigs>>().Value;
            var httpClientFactory = _container.Resolve<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(appConfig.Portals.HttpClientName);
            var expect = new Uri(appConfig.Portals.HttpClientBaseUri);
            Assert.IsTrue(expect == httpClient.BaseAddress);

        }

        [TestMethod]
        public void TestResolveWindowService()
        {
            var windowService = new WinService(
                _container.Resolve<ILogger<WinService>>(), 
                _container.Resolve<IOptions<AppConfigs>>(),
                _container.Resolve<IPortalServices>());
            windowService.ScheduleService();
        }
    }
}
