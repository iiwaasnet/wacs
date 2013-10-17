﻿using System;
using Autofac;
using Autofac.Configuration;
using Topshelf;
using Topshelf.HostConfigurators;
using Topshelf.Runtime;
using wacs.Diagnostics;

namespace wacs
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader());

			var container = builder.Build();

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