using System;
using System.Collections.Generic;

namespace wacs.Configuration
{
    public interface ISynodConfiguration
    {
        TimeSpan ProcessIdBroadcastPeriod { get; }
        IEnumerable<INode> Nodes { get; }
    }
}