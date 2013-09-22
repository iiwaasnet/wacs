using System;
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

		public BestCandidate(int majority)
		{
			this.majority = majority;
			gateway = new ManualResetEventSlim(false);
			votes = new HashSet<string>();
		}

		internal int Vote(Candidate suggestedLeader, Candidate elector)
		{
			if (!suggestedLeader.Equals(this.suggestedLeader))
			{
				this.suggestedLeader = suggestedLeader;
				votes.Clear();
			}

			votes.Add(elector.Id);

			//Console.WriteLine("[{2}] Leader {0} Votes {1}", this.suggestedLeader.Id, votes.Count, DateTime.Now);

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