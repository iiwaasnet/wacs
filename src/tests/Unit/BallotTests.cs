using NUnit.Framework;
using wacs.Messaging.Messages.Intercom.Rsm;

namespace tests.Unit
{
    [TestFixture]
    public class BallotTests
    {
        [Test]
        public void TwoDifferentBallotObjectsWithSameProposalNumber_AreEqual()
        {
            var ballot1 = new Ballot {ProposalNumber = 23};
            var ballot2 = new Ballot {ProposalNumber = ballot1.ProposalNumber};

            Assert.IsTrue(ballot1.Equals(ballot2));
        }
        
        [Test]
        public void TwoSameBallotObjects_AreEqual()
        {
            var ballot1 = new Ballot {ProposalNumber = 23};
            var ballot2 = ballot1;

            Assert.IsTrue(ballot1.Equals(ballot2));
        }

        [Test]
        public void Ballot_IsNotEqualToOtherObject()
        {
            var ballot1 = new Ballot { ProposalNumber = 23 };
            var ballot2 = new { ProposalNumber = ballot1.ProposalNumber };

            Assert.IsFalse(ballot1.Equals(ballot2));
        }

        [Test]
        public void BallotsWithDifferentProposalNumber_AreNotEqual()
        {
            var ballot1 = new Ballot { ProposalNumber = 23 };
            var ballot2 = new Ballot { ProposalNumber = 24 };

            Assert.IsFalse(ballot1.Equals(ballot2));
        }
    }
}