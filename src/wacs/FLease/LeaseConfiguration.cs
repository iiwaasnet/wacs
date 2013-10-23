using System;
using wacs.Configuration;

namespace wacs.FLease
{
	public class LeaseConfiguration : ILeaseConfiguration
	{
		public TimeSpan MaxLeaseTimeSpan { get; set; }
		public TimeSpan ClockDrift { get; set; }
	    public TimeSpan MessageRoundtrip { get; set; }
	}
}