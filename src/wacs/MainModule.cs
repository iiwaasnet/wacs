﻿using System;
using Autofac;
using Topshelf;
using wacs.Diagnostics;
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
            builder.Register(c => new Logger("fileLogger")).As<ILogger>().SingleInstance();
            builder.RegisterType<WACService>().As<ServiceControl>().SingleInstance();
            builder.RegisterType<InprocMessageHub>().As<IMessageHub>().SingleInstance();
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
            builder.Register(c => new PaxosMachine(1,
                                                   c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
            builder.Register(c => new PaxosMachine(2,
                                                   c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
            builder.Register(c => new PaxosMachine(3,
                                                   c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
        }
    }
}