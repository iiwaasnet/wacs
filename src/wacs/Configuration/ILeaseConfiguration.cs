using System;

namespace wacs.Configuration
{
    public interface ILeaseConfiguration
    {
        TimeSpan MaxLeaseTimeSpan { get; }
        TimeSpan ClockDrift { get; }
        TimeSpan MessageRoundtrip { get; }
        TimeSpan NodeResponseTimeout { get; }
    }
}