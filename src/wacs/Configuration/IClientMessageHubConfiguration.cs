using System;

namespace wacs.Configuration
{
    public interface IClientMessageHubConfiguration
    {
        TimeSpan ReceiveWaitTimeout { get; }
        int ParallelMessageProcessors { get; }
    }
}