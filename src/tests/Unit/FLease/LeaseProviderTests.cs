﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.FLease;
using wacs.Resolver;

namespace tests.Unit.FLease
{
    [TestFixture]
    public class LeaseProviderTests
    {
        [Test]
        public void GetLease_ReturnsTask()
        {
            var lease = new Lease(new Process(), DateTime.UtcNow + TimeSpan.FromSeconds(3));
            var leaseProvider = new Mock<ILeaseProvider>();
            leaseProvider.Setup(m => m.GetLease()).Returns(lease);

            var acquiredLease = leaseProvider.Object.GetLease();

            Assert.AreEqual(lease.Owner.Id, acquiredLease.Owner.Id);
            Assert.AreEqual(lease.ExpiresAt, acquiredLease.ExpiresAt);
        }

        [Test]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void LeaseIsIssuedByQuorum(int numberOfNodes)
        {
            var builder = DIHelper.CreateBuilder();
            builder.RegisterType<InprocIntercomMessageHub>().As<IIntercomMessageHub>().SingleInstance();
            var messageHub = builder.Build().Resolve<IIntercomMessageHub>();

            var nodes = new List<INode>();
            var processes = new List<IProcess>();

            for (var i = 0; i < numberOfNodes; i++)
            {
                var node = new Node("tcp://127.0.0.1", 3030 + i, 4030 + i);
                nodes.Add(node);

                var process = new Process();
                processes.Add(process);
            }

            var leaseProviders = new List<ILeaseProvider>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                var setupData = new SetupData
                                {
                                    LocalNode = nodes[i],
                                    Synod = nodes
                                };
                leaseProviders.Add(BuildLeaseProvider(setupData, messageHub));
            }
            var majority = numberOfNodes / 2 + 1;

            //leaseProviders.ForEach(p => p.Start());

            var leases = Enumerable.Empty<ILease>();
            do
            {
                leases = leaseProviders.Select(p => p.GetLease()).ToArray();
            } while (leases.Any(l => l == null));

            Assert.GreaterOrEqual(leases.GroupBy(l => l.ExpiresAt).Max(g => g.Count()), majority);
            Assert.GreaterOrEqual(leases.GroupBy(l => l.Owner.Id).Max(g => g.Count()), majority);

            leaseProviders.ToList().ForEach(p => p.Dispose());
        }

        private ILeaseProvider BuildLeaseProvider(SetupData setupData, IIntercomMessageHub intercomMessageHub)
        {
            var builder = DIHelper.CreateBuilder();

            builder.Register(c => intercomMessageHub).As<IIntercomMessageHub>().SingleInstance();

            var topology = new Mock<ITopologyConfiguration>();
            topology.Setup(m => m.LocalNode).Returns(setupData.LocalNode);

            var synod = new Mock<ISynod>();
            synod.Setup(m => m.Members).Returns(setupData.Synod);
            topology.Setup(m => m.Synod).Returns(synod.Object);

            var leaseConfig = new Mock<ILeaseConfiguration>();
            leaseConfig.Setup(m => m.NodeResponseTimeout).Returns(TimeSpan.FromMilliseconds(200));
            leaseConfig.Setup(m => m.MaxLeaseTimeSpan).Returns(TimeSpan.FromSeconds(2));
            leaseConfig.Setup(m => m.MessageRoundtrip).Returns(TimeSpan.FromMilliseconds(400));
            leaseConfig.Setup(m => m.ClockDrift).Returns(TimeSpan.FromMilliseconds(200));

            var hostResolverConfig = new Mock<INodeResolverConfiguration>();
            hostResolverConfig.Setup(m => m.ProcessIdBroadcastPeriod).Returns(TimeSpan.FromSeconds(1));

            var config = new Mock<IWacsConfiguration>();
            config.Setup(m => m.Lease).Returns(leaseConfig.Object);
            config.Setup(m => m.Topology).Returns(topology.Object);
            config.Setup(m => m.NodeResolver).Returns(hostResolverConfig.Object);

            builder.Register(c => config.Object).As<IWacsConfiguration>();

            var container = builder.Build();

            container.Resolve<INodeResolver>();

            return container.Resolve<ILeaseProvider>();
        }

        internal class SetupData
        {
            internal IEnumerable<INode> Synod { get; set; }
            internal INode LocalNode { get; set; }
        }
    }
}