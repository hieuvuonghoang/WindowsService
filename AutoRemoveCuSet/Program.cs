using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AutoRemoveCuSet.Services;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;

namespace AutoRemoveCuSet
{
    static class Config
    {
        private static ContainerBuilder _containerBuilder = new ContainerBuilder();
        private static IContainer _container;

        public static void Register()
        {
            #region "HttpClientFactory"
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("Portal", (client) =>
            {
                client.BaseAddress = new Uri("https://gis.npt.com.vn");
            });
            _containerBuilder.Populate(serviceCollection);
            #endregion

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

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new WinService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
