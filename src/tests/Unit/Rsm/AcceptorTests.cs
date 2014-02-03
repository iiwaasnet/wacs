using Autofac;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs.Configuration;
using wacs.FLease;

namespace tests.Unit.Rsm
{
    [TestFixture]
    public class AcceptorTests
    {
        [Test]
        public void TestPrepareRequestCameNotFromLeader_IsNotAcknowledged()
        {
            var builder = DIHelper.CreateBuilder();

            var leaseProvider = new Mock<ILeaseProvider>();
            var lease = new Mock<ILease>();
            var leader = new Mock<IProcess>();
            leader.Setup(m => m.Id).Returns(123);
            lease.Setup(m => m.Owner).Returns(leader.Object);
            leaseProvider.Setup(m => m.GetLease()).Returns(lease.Object);

            builder.Register(c => leaseProvider.Object).As<ILeaseProvider>().SingleInstance();
        }
    }
}