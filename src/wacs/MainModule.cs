using Autofac;
using Topshelf;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.zmq;
using wacs.Paxos.Implementation;
using wacs.Paxos.Interface;
using wacs.Resolver.Implementation;
using wacs.Resolver.Interface;

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
            builder.RegisterType<NodeResolver>().As<INodeResolver>().SingleInstance();
            builder.RegisterType<SynodConfigurationProvider>().As<ISynodConfigurationProvider>().SingleInstance();
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
            builder.Register(c => c.Resolve<IWacsConfiguration>().Topology)
                   .As<ITopologyConfiguration>()
                   .SingleInstance();
            builder.Register(c => c.Resolve<IWacsConfiguration>().HostResolver)
                   .As<IHostResolverConfiguration>()
                   .SingleInstance();
        }

        private static void RegisterPaxosInstances(ContainerBuilder builder)
        {
            builder.Register(c => new PaxosMachine(c.Resolve<ILeaseProviderFactory>(),
                                                   c.Resolve<IBallotGenerator>(),
                                                   c.Resolve<INodeResolver>(),
                                                   c.Resolve<ILogger>()))
                   .As<IStateMachine>();
        }
    }
}