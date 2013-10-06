using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using wacs.FLease;
using wacs.Messaging;

namespace wacs
{
	public class MainModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<WACService>().As<IService>().SingleInstance();

			RegisterPaxosInstances(builder);

			builder.RegisterType<LeaseProvider>().As<ILeaseProvider>().SingleInstance();
			builder.RegisterType<RoundBasedRegister>().As<IRoundBasedRegister>().SingleInstance();
			builder.RegisterType<MessageHub>().As<IMessageHub>().SingleInstance();
			builder.RegisterType<BallotGenerator>().As<IBallotGenerator>().SingleInstance();
			builder.RegisterType<MessageSerializer>().As<IMessageSerializer>().SingleInstance();
			builder.Register(c => new FleaseConfiguration
				                      {
					                      ClockDrift = TimeSpan.FromMilliseconds(100),
					                      MaxLeaseTimeSpan = TimeSpan.FromSeconds(5)
				                      })
			       .As<IFleaseConfiguration>()
			       .SingleInstance();
			builder.Register(c => new WacsConfiguration
				                      {
					                      FarmSize = c.Resolve<IEnumerable<IStateMachine>>().Count()
				                      })
			       .As<IWacsConfiguration>()
			       .SingleInstance();
		}

		private static void RegisterPaxosInstances(ContainerBuilder builder)
		{
			builder.Register(c => new PaxosMachine(1, c.Resolve<ILeaseProvider>(), c.Resolve<IWacsConfiguration>())).As<IStateMachine>().SingleInstance();
			builder.Register(c => new PaxosMachine(2, c.Resolve<ILeaseProvider>(), c.Resolve<IWacsConfiguration>())).As<IStateMachine>().SingleInstance();
			builder.Register(c => new PaxosMachine(3, c.Resolve<ILeaseProvider>(), c.Resolve<IWacsConfiguration>())).As<IStateMachine>().SingleInstance();
		}
	}
}