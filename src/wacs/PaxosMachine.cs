using System.Collections.Generic;
using wacs.FLease;

namespace wacs
{
	public class PaxosMachine : IStateMachine
	{
		private readonly int id;
		private readonly List<PaxosMachine> farm;
		private readonly ILeaseProvider leaseProvider;
		private readonly IWacsConfiguration config;

		public PaxosMachine(int id, ILeaseProvider leaseProvider, IWacsConfiguration config)
		{
			this.id = id;
			this.leaseProvider = leaseProvider;
			this.config = config;
			farm = new List<PaxosMachine>();
		}

		public void JoinGroup(IEnumerable<PaxosMachine> group)
		{
			farm.Clear();
			farm.AddRange(group);
		}

		public void Start()
		{
			leaseProvider.Start(new Process(id));
		}

		public void Stop()
		{
			leaseProvider.Stop();
		}

		public int Id
		{
			get { return id; }
		}
	}
}