using System;

namespace wacs.Configuration
{
    public interface INodeResolverConfiguration
    {
        TimeSpan ProcessIdBroadcastPeriod { get; }
    }
}