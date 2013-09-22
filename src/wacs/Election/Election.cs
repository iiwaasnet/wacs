using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace wacs.Election
{
	public class Election : IElection, IElector
	{
		private BestCandidate currentLeader;
		private readonly Candidate self;
		private readonly List<IElector> electors;
		private readonly ManualResetEventSlim electionStarted;

		public Election(Candidate self)
		{
			this.self = self;
			electionStarted = new ManualResetEventSlim(false);
			electors = new List<IElector>();
		}

		public void AddElectors(IEnumerable<IElector> electors)
		{
			this.electors.AddRange(electors);
		}

		public Task<ElectionResult> Elect(TimeSpan timeout)
		{
			return Task.Factory.StartNew(() => StartElectionProcess(timeout));
		}

		private ElectionResult StartElectionProcess(TimeSpan timeout)
		{
			currentLeader = new BestCandidate(CalcMajority());

			electionStarted.Set();

			currentLeader.Vote(self, self);

			ProposeCandidate(self, electors);

			if (currentLeader.ConsensusReached.Wait(timeout))
			{
				Console.WriteLine("Node {0} reached consesus for Leader {1}", self.Id, currentLeader.SuggestedLeader.Id);

				return new ElectionResult
					       {
						       Leader = currentLeader.SuggestedLeader,
						       Status = CampaignStatus.Elected
					       };
			}

			return new ElectionResult {Status = CampaignStatus.Timeout};
		}

		private void ProposeCandidate(Candidate candidate, IEnumerable<IElector> competitors)
		{
			foreach (var competitor in competitors)
			{
				competitor.Propose(candidate);
				competitor.Accepted(candidate, self);
			}
		}

		private static void GetOlder(Candidate self)
		{
			var rnd = new Random((int) DateTime.UtcNow.Ticks & 0x0000ffff);
			self.Age += rnd.Next(1, 100);
		}

		public void Propose(Candidate candidate)
		{
			electionStarted.Wait();

			if (candidate.BetterThan(currentLeader.SuggestedLeader)
				|| candidate.Equals(currentLeader.SuggestedLeader))
			{
				currentLeader.Vote(candidate, self);
				foreach (var competitor in electors)
				{
					competitor.Accepted(candidate, self);
				}
			}
			if (candidate.SameGood(currentLeader.SuggestedLeader))
			{
				if (!candidate.Equals(currentLeader.SuggestedLeader)
				    && currentLeader.SuggestedLeader.Equals(self))
				{
					GetOlder(self);
					ProposeCandidate(self, electors);
				}
			}
			if (candidate.WorseThan(currentLeader.SuggestedLeader))
			{
				ProposeCandidate(currentLeader.SuggestedLeader, electors);
			}

			Console.WriteLine("Propose {0}", candidate.Id);
		}

		public void Accepted(Candidate candidate, Candidate elector)
		{
			electionStarted.Wait();

			if (candidate.BetterThan(currentLeader.SuggestedLeader)
			    || candidate.Equals(currentLeader.SuggestedLeader))
			{
				currentLeader.Vote(candidate, elector);

				Console.WriteLine("Accepted {0}", candidate.Id);
			}
		}

		private int CalcMajority()
		{
			return (electors.Count() + 1) / 2 + 1;
		}
	}
}