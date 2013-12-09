using System;
using System.Diagnostics;
using Autofac;
using Autofac.Configuration;
using Topshelf;
using Topshelf.HostConfigurators;
using Topshelf.Runtime;
using wacs.Diagnostics;
using wacs.Resolver.Interface;

namespace wacs
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ConfigurationSettingsReader());

            var container = builder.Build();


            var resolutionService = container.Resolve<IHostResolver>();

            var timer = new Stopwatch();
            timer.Start();
            resolutionService.Start();
            foreach (var node in resolutionService.GetWorld().Result)
            {
                Console.WriteLine(node.Id);    
            }

            timer.Stop();
            Console.WriteLine("Resolved in {0} msec", timer.ElapsedMilliseconds);
            Console.ReadLine();
            resolutionService.Stop();
            return;

            HostFactory.New(x => ConfigureService(x, container)).Run();
        }

        private static void ConfigureService(HostConfigurator x, IContainer container)
        {
            x.Service<ServiceControl>(s =>
                                      {
                                          s.ConstructUsing(name => container.Resolve<ServiceControl>());
                                          s.WhenStarted((sc, hc) => sc.Start(hc));
                                          s.WhenStopped((sc, hc) => sc.Stop(hc));
                                      });
            x.RunAsLocalSystem();

            x.SetDescription("Wait-free Coordination Service");
            x.SetDisplayName("WACS");
            x.SetServiceName("WACS");

            x.AfterInstall(InstallPerfCounters);
            x.BeforeUninstall(UninstallPerfCounters);
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