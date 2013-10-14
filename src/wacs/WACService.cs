using System;
using System.Collections.Generic;
using Topshelf;

namespace wacs
{
	public class WACService : ServiceControl
	{
		private readonly IEnumerable<IStateMachine> farm;

		public WACService(IEnumerable<IStateMachine> members)
		{
			farm = members;
		}

		public bool Start(HostControl hostControl)
		{
			Console.WriteLine("=== Farm started ======================");
			foreach (var stateMachine in farm)
			{
				stateMachine.Start();

				Console.WriteLine("PAXOS Id: {0}", stateMachine.Id);
			}

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			foreach (var stateMachine in farm)
			{
				stateMachine.Stop();
			}

			return true;
		}
	}
}