using Autofac;
using Autofac.Configuration;

namespace tests.Unit.Helpers
{
	public static class DIHelper
	{
		public static ContainerBuilder CreateBuilder()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader());

			return builder;
		}

		public static IContainer CreateContainer()
		{
			return CreateBuilder().Build();
		}
	}
}