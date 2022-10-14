using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoRemoveCuSet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AutoRemoveCuSet.Services;
using AutoRemoveCuSet.CusLoggingProvider;
using System.ServiceProcess;

namespace AutoRemoveCuSet
{
    static class Config
    {
        private static ContainerBuilder _containerBuilder = new ContainerBuilder();
        private static IContainer _container;

        public static void Register()
        {
            var configuration = new ConfigurationBuilder()
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
                loggingBuilder.ClearProviders();
                loggingBuilder.AddProvider(new NLogLoggerProvider());
            });

            _containerBuilder.Populate(_serviceCollection);

            _containerBuilder.RegisterType<PortalServices>().As<IPortalServices>().SingleInstance();
            
            _container = _containerBuilder.Build();
        }

        public static IContainer Container
        {
            get
            {
                return _container;
            }
        }
    }

    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            Config.Register();

            var winService = new WinService(
                Config.Container.Resolve<ILogger<WinService>>(), 
                Config.Container.Resolve<IOptions<AppConfigs>>(),
                Config.Container.Resolve<IPortalServices>());

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                winService
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
