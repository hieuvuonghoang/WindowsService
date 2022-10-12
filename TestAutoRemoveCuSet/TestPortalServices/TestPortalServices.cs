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
        public void TestLogger()
        {
            var portalServices = _container.Resolve<IPortalServices>();
            portalServices.TestLogger();
        }
    }
}
