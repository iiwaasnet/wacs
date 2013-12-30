using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs;
using wacs.Configuration;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.Inproc;
using wacs.Paxos.Implementation;
using wacs.Resolver.Interface;

namespace tests.Unit.FLease
{
    [TestFixture]
    public class LeaseProviderTests
    {
        [Test]
        public void TestGetLease_ReturnsTask()
        {
            var lease = new Lease(new Process(), DateTime.UtcNow + TimeSpan.FromSeconds(3));
            var leaseProvider = new Mock<ILeaseProvider>();
            leaseProvider.Setup(m => m.GetLease()).Returns(Task.FromResult<ILease>(lease));

            var acquiredLease = leaseProvider.Object.GetLease().Result;

            Assert.AreEqual(lease.Owner.Id, acquiredLease.Owner.Id);
            Assert.AreEqual(lease.ExpiresAt, acquiredLease.ExpiresAt);
        }

        [Test]
        //[TestCase(3)]
        //[TestCase(4)]
        [TestCase(2)]
        public void TestLeaseIsIssuedByQuorum(int numberOfNodes)
        {
            var builder = DIHelper.CreateBuilder();

            builder.RegisterType<InprocMessageHub>().As<IMessageHub>().SingleInstance();
            var config = new Mock<IWacsConfiguration>();
            var leaseConfig = new Mock<ILeaseConfiguration>();
            var topology = new Mock<ITopologyConfiguration>();
            var synod = new Mock<ISynod>();
            leaseConfig.Setup(m => m.NodeResponseTimeout).Returns(TimeSpan.FromSeconds(1));

            var nodeResolver = new Mock<INodeResolver>();
            var nodes = new List<INode>();
            var processes = new List<IProcess>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                var node = new Node(string.Format("tcp://127.0.0.1:303{0}", i));
                nodes.Add(node);
                var process = new Process();
                processes.Add(process);

                nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == process.Id))).Returns(node);
            }

            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            topology.Setup(m => m.LocalNode).Returns(nodes[0]);

            synod.Setup(m => m.Members).Returns(nodes);
            topology.Setup(m => m.Synod).Returns(synod.Object);
            config.Setup(m => m.Lease).Returns(leaseConfig.Object);
            config.Setup(m => m.Topology).Returns(topology.Object);

            builder.Register(c => config.Object).As<IWacsConfiguration>().SingleInstance();

            var container = builder.Build();
            var leaseProviderFactory = container.Resolve<ILeaseProviderFactory>();

            var leaseProviders = new List<ILeaseProvider>();
            processes.ForEach(p => leaseProviders.Add(leaseProviderFactory.Build(p)));
            //for (var i = 0; i < numberOfNodes; i++)
            //{
            //    var process = new Process();
            //    leaseProviders.Add(leaseProviderFactory.Build(process));
            //}

            var majority = numberOfNodes / 2 + 1;

            leaseProviders.ForEach(p => p.Start());

            var leases = leaseProviders.Select(p => p.GetLease().Result).ToArray();

            Assert.GreaterOrEqual(leases.GroupBy(l => l.ExpiresAt).Max(g => g.Count()), majority);
            Assert.GreaterOrEqual(leases.GroupBy(l => l.Owner.Id).Max(g => g.Count()), majority);

            leaseProviders.ToList().ForEach(p => p.Stop());
        }

        [Test]
        public void TestStartingLeaseProvider_StartsRoundBasedRegister()
        {
            var builder = DIHelper.CreateBuilder();

            builder.RegisterType<InprocMessageHub>().As<IMessageHub>().SingleInstance();
            var owner = new Process();
            var register = new Mock<IRoundBasedRegister>();
            var registerFactory = new Mock<IRoundBasedRegisterFactory>();
            registerFactory.Setup(m => m.Build(It.Is<IProcess>(v => v.Id == owner.Id))).Returns(register.Object);

            builder.Register(c => registerFactory.Object).As<IRoundBasedRegisterFactory>().SingleInstance();

            var leaseProvider = builder.Build().Resolve<ILeaseProviderFactory>().Build(owner);

            leaseProvider.Start();

            register.Verify(m => m.Start(), Times.Once());
        }

        [Test]
        public void TestStopingLeaseProvider_StopsRoundBasedRegister()
        {
            var builder = DIHelper.CreateBuilder();

            builder.RegisterType<InprocMessageHub>().As<IMessageHub>().SingleInstance();
            var owner = new Process();
            var register = new Mock<IRoundBasedRegister>();
            var registerFactory = new Mock<IRoundBasedRegisterFactory>();
            registerFactory.Setup(m => m.Build(It.Is<IProcess>(v => v.Id == owner.Id))).Returns(register.Object);

            builder.Register(c => registerFactory.Object).As<IRoundBasedRegisterFactory>().SingleInstance();

            var leaseProvider = builder.Build().Resolve<ILeaseProviderFactory>().Build(owner);

            leaseProvider.Stop();

            register.Verify(m => m.Stop(), Times.Once());
        }
    }
}