using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wacs.Election;

namespace wacs
{
	public class PaxosMachine : IElector
	{
		private readonly string id;
		private readonly List<PaxosMachine> farm;
		private readonly Election.Election election;
		private readonly long age;
		private readonly long lastAppliedLogEntry;

		public PaxosMachine(string id, long lastAppliedLogEntry)
		{
			this.id = id;
			age = DateTime.UtcNow.Ticks;
			this.lastAppliedLogEntry = lastAppliedLogEntry;
			farm = new List<PaxosMachine>();
			election = new Election.Election(new Candidate {Id = id, Age = age, LastAppliedLogEntry = lastAppliedLogEntry});
		}

		public Task<ElectionResult> ElectLeader(TimeSpan timeout)
		{
			return election.Elect(timeout);
		}

		public void Propose(Candidate candidate)
		{
			election.Propose(candidate);
		}

		public void Accepted(Candidate candidate, Candidate elector)
		{
			election.Accepted(candidate, elector);
		}

		public void JoinGroup(IEnumerable<PaxosMachine> group)
		{
			farm.Clear();
			farm.AddRange(group);
			election.SetElectors(group);
		}

		public void Stop()
		{
			election.Stop();
		}

		public string Id
		{
			get { return id; }
		}

		public long Age
		{
			get { return age; }
		}

		public long LastAppliedLogEntry
		{
			get { return lastAppliedLogEntry; }
		}
	}
}