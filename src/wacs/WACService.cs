using System;
using System.Collections.Generic;
using wacs.FLease;

namespace wacs
{
	public class Test
	{
		public DateTime Timestamp { get; set; }
	}

	public class WACService : IService
	{
		private readonly IEnumerable<IStateMachine> farm;

		public WACService(IEnumerable<IStateMachine> members)
		{
			farm = members;
		}

		public void Start()
		{
			Console.WriteLine("=== Farm started ======================");
			foreach (var stateMachine in farm)
			{
				stateMachine.Start();

				Console.WriteLine("PAXOS Id: {0}", stateMachine.Id);
			}
		}

		public void Stop()
		{
			foreach (var stateMachine in farm)
			{
				stateMachine.Stop();
			}
		}
	}
}