using System;
using Autofac;
using Autofac.Configuration;
using Topshelf.HostConfigurators;
using Topshelf.Runtime;
using Topshelf.ServiceConfigurators;
using wacs.Diagnostics;
using HostControl = Topshelf.HostControl;
using HostFactory = Topshelf.HostFactory;
using InstallHostConfiguratorExtensions = Topshelf.InstallHostConfiguratorExtensions;
using RunAsExtensions = Topshelf.RunAsExtensions;
using ServiceConfiguratorExtensions = Topshelf.ServiceConfiguratorExtensions;
using ServiceControl = Topshelf.ServiceControl;
using ServiceExtensions = Topshelf.ServiceExtensions;
using UninstallHostConfiguratorExtensions = Topshelf.UninstallHostConfiguratorExtensions;

namespace wacs
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ConfigurationSettingsReader());

            var container = builder.Build();

            //var resolutionService = container.Resolve<INodeResolver>();

            //var timer = new Stopwatch();
            //timer.Start();
            //resolutionService.Start();
            //foreach (var process in resolutionService.GetWorld())
            //{
            //    Console.WriteLine(process.Id);    
            //}

            //timer.Stop();
            //Console.WriteLine("Resolved in {0} msec", timer.ElapsedMilliseconds);
            //Console.ReadLine();
            //resolutionService.Stop();
            //return;

            HostFactory.New(x => ConfigureService(x, container)).Run();
        }

        private static void ConfigureService(HostConfigurator x, IContainer container)
        {
            ServiceExtensions.Service<ServiceControl>(x, s => SetupServiceControl(container, s));
            RunAsExtensions.RunAsLocalSystem(x);

            x.SetDescription("Wait-free Coordination Service");
            x.SetDisplayName("WACS");
            x.SetServiceName("WACS");

            InstallHostConfiguratorExtensions.AfterInstall(x, InstallPerfCounters);
            UninstallHostConfiguratorExtensions.BeforeUninstall(x, UninstallPerfCounters);
        }

        private static void SetupServiceControl(IContainer container, ServiceConfigurator<ServiceControl> s)
        {
            s.ConstructUsing(name => container.Resolve<ServiceControl>());
            s.WhenStarted((sc, hc) => sc.Start(hc));
            s.WhenStopped((sc, hc) => StopService(sc, hc, container));
            ServiceConfiguratorExtensions.WhenStopped(s, sc => container.Dispose());
        }

        private static bool StopService(ServiceControl sc, HostControl hc, IContainer container)
        {
            if (sc.Stop(hc))
            {
                container.Dispose();
                return true;
            }

            return false;
        }

        private static void UninstallPerfCounters()
        {
            try
            {
                new PerformanceCountersInstaller<WacsPerformanceCounters>().Uninstall();
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
        }

        private static void InstallPerfCounters(InstallHostSettings settings)
        {
            try
            {
                new PerformanceCountersInstaller<WacsPerformanceCounters>().Install();
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
        }
    }
}