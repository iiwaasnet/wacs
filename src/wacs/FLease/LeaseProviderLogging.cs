using System;

namespace wacs.FLease
{
	public partial class LeaseProvider
	{
		private void LogAwake()
		{
			logger.DebugFormat("SLEEP === Process {0} Waked up at {1}", owner.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));
		}

		private void LogStartSleep()
		{
			logger.DebugFormat("SLEEP === Process {0} Sleep from {1}", owner.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));
		}
	}
}