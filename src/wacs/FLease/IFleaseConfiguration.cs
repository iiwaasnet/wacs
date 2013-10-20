using System;

namespace wacs.FLease
{
    public interface IFleaseConfiguration
    {
        TimeSpan MaxLeaseTimeSpan { get; }
        TimeSpan ClockDrift { get; }
        TimeSpan MessageRoundtrip { get; }
    }
}