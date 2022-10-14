using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoRemoveCuSet;
using AutoRemoveCuSet.CusLoggingProvider;
using AutoRemoveCuSet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TestAutoRemoveCuSet.TestPortalServices
{
    [TestClass]
    public class TestPortalServices
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
            _serviceCollection.AddLogging((configure) =>
            {
                configure.ClearProviders();
                configure.AddProvider(new NLogLoggerProvider());
            });

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(_serviceCollection);

            containerBuilder.RegisterType<PortalServices>().As<IPortalServices>().SingleInstance();

            _container = containerBuilder.Build();
        }

        [TestMethod]
        public async Task TestGenerateTokenAsync()
        {
            var portalServices = _container.Resolve<IPortalServices>();
            var accessToken = await portalServices.GeneratePortalTokeAsync();
            Assert.IsNotNull(accessToken);
        }

        [TestMethod]
        public async Task TestRemoveFeatureCuSet()
        {
            var portalServices = _container.Resolve<IPortalServices>();
            var thoiGianCuSet = new DateTime(2022, 10, 12, 06, 59, 59);
            var accessToken = await portalServices.GeneratePortalTokeAsync();
            await portalServices.RemoveFeatureCuSet(AutoRemoveCuSet.Models.CuSetType.CuSet1Ngay, thoiGianCuSet, accessToken);
        }
    }
}
