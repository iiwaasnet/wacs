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
			x.Service<IService>(s =>
				                    {
					                    s.ConstructUsing(name => container.Resolve<IService>());
					                    s.WhenStarted(tc => tc.Start());
					                    s.WhenStopped(tc => tc.Stop());
				                    });
			x.RunAsLocalSystem();

			x.SetDescription("Wait-free Coordination Service");
			x.SetDisplayName("WACS");
			x.SetServiceName("WACS");
		}
	}
}