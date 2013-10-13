﻿using Autofac;
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
	}
}