using System;
using System.Net;
using System.ServiceModel;
using Autofac;
using Topshelf;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.Inproc;

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
            builder.Register(c => new Logger("fileLogger")).As<ILogger>().SingleInstance();
            builder.RegisterType<WACService>().As<ServiceControl>().SingleInstance();
            builder.RegisterType<InprocMessageHub>().As<IMessageHub>().SingleInstance();
            builder.RegisterType<MessageSerializer>().As<IMessageSerializer>().SingleInstance();
            RegisterConfigurations(builder);
        }

        private static void RegisterConfigurations(ContainerBuilder builder)
        {
            builder.Register(c => new FleaseConfiguration
                                  {
                                      ClockDrift = TimeSpan.FromMilliseconds(100),
                                      MaxLeaseTimeSpan = TimeSpan.FromSeconds(3),
                                      MessageRoundtrip = TimeSpan.FromMilliseconds(200)
                                  })
                   .As<IFleaseConfiguration>()
                   .SingleInstance();
            builder.Register(c => new WacsConfiguration
                                  {
                                      FarmSize = FarmSize
                                  })
                   .As<IWacsConfiguration>()
                   .SingleInstance();
            builder.Register(c => new MessageHubConfiguration {
                Sender = new EndpointAddress("tcp://127.0.0.1:5050")
            })
                   .As<IMessageHubConfiguration>()
                   .SingleInstance();
        }

        private static void RegisterPaxosInstances(ContainerBuilder builder)
        {
            builder.Register(c => new PaxosMachine(1.ToString(),
                                                   c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
            builder.Register(c => new PaxosMachine(2.ToString(),
                                                   c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
            builder.Register(c => new PaxosMachine(3.ToString(),
                                                   c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
        }
    }
}