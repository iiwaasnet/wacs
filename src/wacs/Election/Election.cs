﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace wacs.Election
{
	public class Election : IElection, IElector
	{
		private BestCandidate currentLeader;
		private readonly Candidate self;
		private readonly List<IElector> electors;
		private readonly BlockingCollection<ProposeMessage> proposesQueue;
		private readonly BlockingCollection<AcceptMessage> acceptsQueue;
		private bool running;
		private readonly AutoResetEvent electionTrigger;
		private readonly AutoResetEvent resultAvailable;
		private TimeSpan timeout;
		private ElectionResult electionResult;

		public Election(Candidate self)
		{
			this.self = self;
			electors = new List<IElector>();
			proposesQueue = new BlockingCollection<ProposeMessage>(new ConcurrentQueue<ProposeMessage>());
			acceptsQueue = new BlockingCollection<AcceptMessage>(new ConcurrentQueue<AcceptMessage>());

			running = true;
			electionTrigger = new AutoResetEvent(false);
			resultAvailable = new AutoResetEvent(false);
			new Thread(ProcessAcceptMessages).Start();
			new Thread(ProcessProposeMessages).Start();
			new Thread(ElectionEventLoop).Start();
		}

		private void ElectionEventLoop()
		{
			while (running)
			{
				try
				{
					electionTrigger.WaitOne();
					electionResult = StartElectionProcess(timeout);
					resultAvailable.Set();
				}
				catch (Exception err)
				{
					Console.WriteLine(err);
				}
			}
		}

		public void SetElectors(IEnumerable<IElector> electors)
		{
			this.electors.Clear();
			this.electors.AddRange(electors);
			currentLeader = new BestCandidate(self, GetQuorum());
		}

		public WaitHandle Elect(TimeSpan timeout)
		{
			this.timeout = timeout;
			electionTrigger.Set();
			return resultAvailable;

			//return Task.Factory.StartNew(() => StartElectionProcess(timeout));
		}

		private ElectionResult StartElectionProcess(TimeSpan timeout)
		{
			ProposeCandidate(self, electors);

			if (currentLeader.ConsensusReached.Wait(timeout))
			{
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
			}
		}

		private static void GetOlder(Candidate self)
		{
			var rnd = new Random((int) DateTime.UtcNow.Ticks & 0x0000ffff);
			self.Age += rnd.Next(1, 100);
		}

		public void Propose(Candidate candidate)
		{
			if (!currentLeader.ConsensusReached.IsSet)
			{
				proposesQueue.Add(new ProposeMessage {Candidate = candidate});
			}
		}

		public void Accepted(Candidate candidate, Candidate elector)
		{
			if (!currentLeader.ConsensusReached.IsSet)
			{
				acceptsQueue.Add(new AcceptMessage {Candidate = candidate, Elector = elector});
			}
		}

		private void ProcessProposeMessages()
		{
			foreach (var proposeMessage in proposesQueue.GetConsumingEnumerable())
			{
				ProcessPropose(proposeMessage);
			}
		}

		private void ProcessPropose(ProposeMessage message)
		{
			var candidate = message.Candidate;

			if (candidate.BetterThan(currentLeader.SuggestedLeader)
			    || candidate.Equals(currentLeader.SuggestedLeader))
			{
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
			//if (candidate.WorseThan(currentLeader.SuggestedLeader))
			//{
			//	ProposeCandidate(currentLeader.SuggestedLeader, electors);
			//}
		}

		private void ProcessAccept(AcceptMessage acceptMessage)
		{
			var candidate = acceptMessage.Candidate;
			var elector = acceptMessage.Elector;

			if (candidate.BetterThan(currentLeader.SuggestedLeader)
			    || candidate.Equals(currentLeader.SuggestedLeader))
			{
				currentLeader.Vote(candidate, elector);
			}
		}

		private void ProcessAcceptMessages()
		{
			foreach (var acceptMessage in acceptsQueue.GetConsumingEnumerable())
			{
				ProcessAccept(acceptMessage);
			}
		}

		private int GetQuorum()
		{
			return Math.Max(GetMajority(), electors.Count() - 1);
		}

		private int GetMajority()
		{
			return electors.Count() / 2 + 1;
		}

		public void Stop()
		{
			acceptsQueue.CompleteAdding();
			proposesQueue.CompleteAdding();
			electionTrigger.Dispose();
			running = false;
		}

		public ElectionResult GetElectionResult()
		{
			return electionResult;
		}
	}
}