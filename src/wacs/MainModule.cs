using Autofac;

namespace wacs
{
	public class MainModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<WACService>().As<IService>().SingleInstance();
		}
	}
}