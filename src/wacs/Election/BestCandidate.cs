using System.Collections.Generic;
using System.Threading;

namespace wacs.Election
{
	internal class BestCandidate
	{
		private Candidate suggestedLeader;
		private readonly HashSet<string> votes;
		private readonly int majority;
		private readonly ManualResetEventSlim gateway;

		public BestCandidate(Candidate candidate, int majority)
		{
			this.majority = majority;
			gateway = new ManualResetEventSlim(false);
			votes = new HashSet<string>();
			suggestedLeader = candidate;
		}

		internal int Vote(Candidate candidate, Candidate elector)
		{
			if (!suggestedLeader.Equals(candidate))
			{
				suggestedLeader = candidate;
				votes.Clear();
			}

			votes.Add(elector.Id);

			if (votes.Count == majority)
			{
				gateway.Set();
			}

			return votes.Count;
		}

		internal Candidate SuggestedLeader
		{
			get { return suggestedLeader; }
		}

		internal ManualResetEventSlim ConsensusReached
		{
			get { return gateway; }
		}
	}
}