using Autofac;
using Autofac.Configuration;
using Topshelf;
using Topshelf.HostConfigurators;

namespace wacs
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader());

			var container = builder.Build();

			HostFactory.Run(x => ConfigureService(x, container));
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
		}
	}
}