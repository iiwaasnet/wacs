using System;

namespace wacs.Configuration
{
    public interface IHostResolverConfiguration
    {
        TimeSpan ProcessIdBroadcastPeriod { get; }
    }
}