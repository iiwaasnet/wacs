using System;
using System.Collections.Generic;
using Topshelf;
using wacs.Diagnostics;

namespace wacs
{
	public class WACService : ServiceControl
	{
		private readonly IEnumerable<IStateMachine> farm;
		private readonly ILogger logger;

		public WACService(IEnumerable<IStateMachine> members, ILogger logger)
		{
			farm = members;
			this.logger = logger;
		}

		public bool Start(HostControl hostControl)
		{
			foreach (var stateMachine in farm)
			{
				stateMachine.Start();

				logger.InfoFormat("WACS Id:[{0}] started", stateMachine.Id);
			}

			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			foreach (var stateMachine in farm)
			{
				stateMachine.Stop();

				logger.InfoFormat("WACS Id:[{0}] stopped", stateMachine.Id);
			}

			return true;
		}
	}
}