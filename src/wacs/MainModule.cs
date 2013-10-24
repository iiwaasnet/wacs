using System;
using Autofac;
using Topshelf;
using wacs.Configuration;
using wacs.core;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.Inproc;
using wacs.Messaging.zmq;

namespace wacs
{
    public class MainModule : Module
    {
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
            builder.RegisterType<MessageHub>().As<IMessageHub>().SingleInstance();
            builder.RegisterType<MessageSerializer>().As<IMessageSerializer>().SingleInstance();
            RegisterConfigurations(builder);
        }

        private static void RegisterConfigurations(ContainerBuilder builder)
        {
            builder.Register(c => SimpleConfigSections.Configuration.Get<IWacsConfiguration>())
                   .As<IWacsConfiguration>()
                   .SingleInstance();

            builder.Register(c => c.Resolve<IWacsConfiguration>().Lease)
                   .As<ILeaseConfiguration>()
                   .SingleInstance();
            builder.Register(c => c.Resolve<IWacsConfiguration>().Synod)
                   .As<ISynodConfiguration>()
                   .SingleInstance();
        }

        private static void RegisterPaxosInstances(ContainerBuilder builder)
        {
            builder.Register(c => new PaxosMachine(c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
            //builder.Register(c => new PaxosMachine(2.ToString(),
            //                                       c.Resolve<ILeaseProviderFactory>(),
            //                                       c.Resolve<IBallotGenerator>(),
            //                                       c.Resolve<ILogger>()))
            //       .As<IStateMachine>();
            //builder.Register(c => new PaxosMachine(3.ToString(),
            //                                       c.Resolve<ILeaseProviderFactory>(),
            //                                       c.Resolve<IBallotGenerator>(),
            //                                       c.Resolve<ILogger>()))
            //       .As<IStateMachine>();
        }
    }
}