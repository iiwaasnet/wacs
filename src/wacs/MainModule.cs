using Autofac;
using Topshelf;
using wacs.Communication.Hubs.Client;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging.Messages;
using wacs.Resolver;
using wacs.Rsm.Implementation;
using wacs.Rsm.Interface;

namespace wacs
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LeaseProvider>().As<ILeaseProvider>().SingleInstance();
            builder.RegisterType<RoundBasedRegister>().As<IRoundBasedRegister>().SingleInstance();
            builder.RegisterType<ClientMessageProcessor>().As<IClientMessageProcessor>().SingleInstance();
            builder.RegisterType<ClientMessagesRepository>().As<IClientMessagesRepository>().SingleInstance();
            builder.RegisterType<ClientMessageHub>().As<IClientMessageHub>().SingleInstance();
            builder.RegisterType<ClientMessageRouter>().As<IClientMessageRouter>().SingleInstance();
            builder.RegisterType<Rsm.Implementation.Rsm>().As<IRsm>().SingleInstance();
            builder.RegisterType<ReplicatedLog>().As<IReplicatedLog>().SingleInstance();
            builder.RegisterType<ConsensusFactory>().As<IConsensusFactory>().SingleInstance();
            builder.RegisterType<ConsensusRoundManager>().As<IConsensusRoundManager>().SingleInstance();

            builder.RegisterType<BallotGenerator>().As<IBallotGenerator>().SingleInstance();
            builder.RegisterType<Bootstrapper>().As<IBootstrapper>().SingleInstance();
            builder.Register(c => new Logger("fileLogger")).As<ILogger>().SingleInstance();
            builder.RegisterType<WACService>().As<ServiceControl>().SingleInstance();
            builder.RegisterType<IntercomMessageHub>().As<IIntercomMessageHub>().SingleInstance();
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
            builder.Register(c => c.Resolve<IWacsConfiguration>().NodeResolver)
                   .As<INodeResolverConfiguration>()
                   .SingleInstance();
            builder.Register(c => c.Resolve<IWacsConfiguration>().ClientMessageHub)
                   .As<IClientMessageHubConfiguration>()
                   .SingleInstance();
            builder.Register(c => c.Resolve<IWacsConfiguration>().Rsm)
                   .As<IRsmConfiguration>()
                   .SingleInstance();
        }
    }
}