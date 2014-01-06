using Autofac;
using Topshelf;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.zmq;
using wacs.Resolver.Implementation;
using wacs.Resolver.Interface;
using wacs.Rsm.Implementation;
using wacs.Rsm.Interface;

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
            builder.RegisterType<PaxosMachine>().As<IStateMachine>().SingleInstance();

            builder.RegisterType<LeaseProvider>().As<ILeaseProvider>();
            builder.RegisterType<RoundBasedRegister>().As<IRoundBasedRegister>();

            builder.RegisterType<BallotGenerator>().As<IBallotGenerator>();
        }

        private void RegisterSingletons(ContainerBuilder builder)
        {
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
        }
    }
}