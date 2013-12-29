using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs;
using wacs.FLease;
using wacs.Messaging;
using wacs.Messaging.Inproc;

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
        [TestCase(3)]
        [TestCase(4)]
        public void TestLeaseIsIssuedByQuorum(int numberOfNodes)
        {
            var builder = DIHelper.CreateBuilder();

            builder.RegisterType<InprocMessageHub>().As<IMessageHub>().SingleInstance();

            var container = builder.Build();
            var leaseProviderFactory = container.Resolve<ILeaseProviderFactory>();

            var leaseProviders = new List<ILeaseProvider>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                leaseProviders.Add(leaseProviderFactory.Build(new Process()));
            }

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