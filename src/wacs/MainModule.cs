using System;
using Autofac;
using wacs.FLease;
using wacs.Messaging;

namespace wacs
{
	public class MainModule : Module
	{
		private const int FarmSize = 3;

		protected override void Load(ContainerBuilder builder)
		{
			RegisterSingletons(builder);
			RegisterPerInstance(builder);
		}

		private void RegisterPerInstance(ContainerBuilder builder)
		{
			RegisterPaxosInstances(builder);

			builder.RegisterType<LeaseProviderFactory>().As<ILeaseProviderFactory>();
			builder.RegisterType<RoundBasedRegisterFactory>().As<IRoundBasedRegisterFactory>();

			builder.RegisterType<BallotGenerator>().As<IBallotGenerator>();
		}

		private void RegisterSingletons(ContainerBuilder builder)
		{
			builder.RegisterType<WACService>().As<IService>().SingleInstance();
			builder.RegisterType<MessageHub>().As<IMessageHub>().SingleInstance();
			builder.RegisterType<MessageSerializer>().As<IMessageSerializer>().SingleInstance();
			builder.Register(c => new FleaseConfiguration
				                      {
					                      ClockDrift = TimeSpan.FromMilliseconds(100),
					                      MaxLeaseTimeSpan = TimeSpan.FromSeconds(3)
				                      })
			       .As<IFleaseConfiguration>()
			       .SingleInstance();
			builder.Register(c => new WacsConfiguration
				                      {
					                      FarmSize = FarmSize
				                      })
			       .As<IWacsConfiguration>()
			       .SingleInstance();
		}

		private static void RegisterPaxosInstances(ContainerBuilder builder)
		{
			builder.Register(c => new PaxosMachine(1, c.Resolve<ILeaseProviderFactory>(), c.Resolve<IBallotGenerator>())).As<IStateMachine>();
			builder.Register(c => new PaxosMachine(2, c.Resolve<ILeaseProviderFactory>(), c.Resolve<IBallotGenerator>())).As<IStateMachine>();
			builder.Register(c => new PaxosMachine(3, c.Resolve<ILeaseProviderFactory>(), c.Resolve<IBallotGenerator>())).As<IStateMachine>();
			//builder.Register(c => new PaxosMachine(4, c.Resolve<ILeaseProvider>(), c.Resolve<IBallotGenerator>())).As<IStateMachine>();
			//builder.Register(c => new PaxosMachine(5, c.Resolve<ILeaseProvider>(), c.Resolve<IBallotGenerator>())).As<IStateMachine>();
		}
	}
}