using System;

namespace wacs.FLease
{
	public class FleaseConfiguration : IFleaseConfiguration
	{
		public TimeSpan MaxLeaseTimeSpan { get; set; }
		public TimeSpan ClockDrift { get; set; }
	    public TimeSpan MessageRoundtrip { get; set; }
	}
}