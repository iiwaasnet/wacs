using System;
using Autofac;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs;
using wacs.Configuration;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using wacs.Resolver;
using wacs.Rsm.Implementation;
using Ballot = wacs.FLease.Ballot;

namespace tests.Unit.FLease
{
    [TestFixture]
    public class RoundBasedRegisterTests
    {
        private ContainerBuilder builder;
        private INode node;

        [SetUp]
        public void Setup()
        {
            builder = DIHelper.CreateBuilder();

            builder.RegisterType<InprocIntercomMessageHub>().As<IIntercomMessageHub>().SingleInstance();
            var config = new Mock<IWacsConfiguration>();
            var leaseConfig = new Mock<ILeaseConfiguration>();
            var topology = new Mock<ITopologyConfiguration>();
            var synod = new Mock<ISynod>();
            leaseConfig.Setup(m => m.NodeResponseTimeout).Returns(TimeSpan.FromSeconds(1));
            node = CreateLocalNode();
            topology.Setup(m => m.LocalNode).Returns(node);
            synod.Setup(m => m.Members).Returns(new[] {topology.Object.LocalNode});
            topology.Setup(m => m.Synod).Returns(synod.Object);
            config.Setup(m => m.Lease).Returns(leaseConfig.Object);
            config.Setup(m => m.Topology).Returns(topology.Object);

            builder.Register(c => config.Object).As<IWacsConfiguration>().SingleInstance();
        }

        [Test(Description = "Lemma R1: Read-abort")]
        public void TestReadWithLowerBallotIsRejected_ByPreviousReadWithHigherBallot()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {
                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                Assert.AreEqual(TxOutcome.Commit, register.Read(ballot1).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Read(ballot).TxOutcome);

            }
        }

        [Test(Description = "Lemma R1: Read-abort")]
        public void TestReadWithLowerBallotIsRejected_ByPreviousWriteWithHigherBallot()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                Assert.AreEqual(TxOutcome.Commit, register.Write(ballot1, new Lease(owner, DateTime.UtcNow)).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Read(ballot).TxOutcome);

            }
        }

        [Test(Description = "Lemma R2: Write-abort")]
        public void TestWriteWithLowerBallotIsRejected_ByPreviousReadWithHigherBallot()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                Assert.AreEqual(TxOutcome.Commit, register.Read(ballot1).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Write(ballot, new Lease(owner, DateTime.UtcNow)).TxOutcome);

            }
        }

        [Test(Description = "Lemma R2: Write-abort")]
        public void TestWriteWithLowerBallotIsRejected_ByPreviousWriteWithHigherBallot()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                Assert.AreEqual(TxOutcome.Commit, register.Write(ballot1, new Lease(owner, DateTime.UtcNow)).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Write(ballot, new Lease(owner, DateTime.UtcNow)).TxOutcome);

            }
        }

        [Test(Description = "Lemma R3: Read-write-commit")]
        public void TestIfReadWithHigherBallotCommits_ThenReadWithLowerOrEqualBallotAborts()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                Assert.AreEqual(TxOutcome.Commit, register.Read(ballot1).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Read(ballot).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Read(ballot1).TxOutcome);

            }
        }

        [Test(Description = "Lemma R3: Read-write-commit")]
        public void TestIfWriteWithHigherBallotCommits_ThenWriteWithLowerBallotAborts()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                Assert.AreEqual(TxOutcome.Commit, register.Write(ballot1, new Lease(owner, DateTime.UtcNow)).TxOutcome);
                Assert.AreEqual(TxOutcome.Abort, register.Write(ballot, new Lease(owner, DateTime.UtcNow)).TxOutcome);

            }
        }

        [Test(Description = "Lemma R4: Read-commit")]
        public void TestIfReadWithHigherBallotCommitsWithL1_ThenWriteWithLowerBallotCommitedWithL1Before()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);

                Assert.IsTrue(ballot1 > ballot);

                var lease = new Lease(owner, DateTime.UtcNow);
                Assert.AreEqual(TxOutcome.Commit, register.Write(ballot, lease).TxOutcome);
                var readLease = register.Read(ballot1);
                Assert.AreEqual(TxOutcome.Commit, readLease.TxOutcome);
                Assert.AreEqual(lease.Owner.Id, readLease.Lease.Owner.Id);
                Assert.AreEqual(lease.ExpiresAt, readLease.Lease.ExpiresAt);

            }
        }

        [Test(Description = "Lemma R5: Write-commit")]
        public void TestIfTwoWritesCommitWithL1AndL2_ThenReadWithHigherBallotCommitsWithL2()
        {
            var owner = new Process();

            var nodeResolver = new Mock<INodeResolver>();
            nodeResolver.Setup(m => m.ResolveRemoteProcess(It.Is<IProcess>(p => p.Id == owner.Id))).Returns(node);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(owner);
            builder.Register(c => nodeResolver.Object).As<INodeResolver>().SingleInstance();

            using (var register = builder.Build().Resolve<IRoundBasedRegister>())
            {

                var ballot = new Ballot(DateTime.UtcNow, 0, owner);
                var ballot1 = new Ballot(DateTime.UtcNow, 1, owner);
                var ballot3 = new Ballot(DateTime.UtcNow, 3, owner);

                Assert.IsTrue(ballot1 > ballot);

                var lease1 = new Lease(owner, DateTime.UtcNow);
                var lease2 = new Lease(owner, DateTime.UtcNow + TimeSpan.FromSeconds(2));

                Assert.AreEqual(TxOutcome.Commit, register.Write(ballot, lease1).TxOutcome);
                Assert.AreEqual(TxOutcome.Commit, register.Write(ballot1, lease2).TxOutcome);
                var readLease = register.Read(ballot3);
                Assert.AreEqual(TxOutcome.Commit, readLease.TxOutcome);
                Assert.AreEqual(lease2.Owner.Id, readLease.Lease.Owner.Id);
                Assert.AreEqual(lease2.ExpiresAt, readLease.Lease.ExpiresAt);

            }
        }

        private static Node CreateLocalNode()
        {
            return new Node("tcp://127.0.0.1", 3030, 4030);
        }
    }
}