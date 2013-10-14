using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using wacs.FLease;
using wacs.Tests.Helpers;

namespace wacs.Tests.FLease
{
	[TestFixture]
	public class LeaseProviderTests
	{
		[Test]
		public void TestStartingLeaseProvider_StartsRoundBasedRegister()
		{
			var builder = DIHelper.CreateBuilder();

			var owner = new Process(12);
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

			var owner = new Process(12);
			var register = new Mock<IRoundBasedRegister>();
			var registerFactory = new Mock<IRoundBasedRegisterFactory>();
			registerFactory.Setup(m => m.Build(It.Is<IProcess>(v => v.Id == owner.Id))).Returns(register.Object);

			builder.Register(c => registerFactory.Object).As<IRoundBasedRegisterFactory>().SingleInstance();

			var leaseProvider = builder.Build().Resolve<ILeaseProviderFactory>().Build(owner);

			leaseProvider.Stop();

			register.Verify(m => m.Stop(), Times.Once());
		}

		[Test]
		public void TestGetLease_ReturnsTask()
		{
			var lease = new Lease(new Process(12), DateTime.UtcNow + TimeSpan.FromSeconds(3));
			var leaseProvider = new Mock<ILeaseProvider>();
			leaseProvider.Setup(m => m.GetLease()).Returns(Task.FromResult<ILease>(lease));

			var acquiredLease = leaseProvider.Object.GetLease().Result;

			Assert.AreEqual(lease.Owner.Id, acquiredLease.Owner.Id);
			Assert.AreEqual(lease.ExpiresAt, acquiredLease.ExpiresAt);
		}

		[Test]
		[TestCase(3)]
		[TestCase(4)]
		public void LeaseIsIssuedByQuorum(int numberOfNodes)
		{
			var container = DIHelper.CreateContainer();

			var leaseProviderFactory = container.Resolve<ILeaseProviderFactory>();

			var leaseProviders = new List<ILeaseProvider>();
			for (var i = 0; i < numberOfNodes; i++)
			{
				leaseProviders.Add(leaseProviderFactory.Build(new Process(i)));
			}

			var majority = numberOfNodes / 2 + 1;

			leaseProviders.ForEach(p => p.Start());

			var leases = leaseProviders.Select(p => p.GetLease().Result).ToArray();

			Assert.IsTrue(leases.GroupBy(l => l.ExpiresAt).Any(g => g.Count() >= majority));
			Assert.IsTrue(leases.GroupBy(l => l.Owner.Id).Any(g => g.Count() >= majority));

			leaseProviders.ToList().ForEach(p => p.Stop());
		}
	}
}