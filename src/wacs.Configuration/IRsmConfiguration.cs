using System;

namespace wacs.Configuration
{
    public interface IRsmConfiguration
    {
        TimeSpan CommandExecutionTimeout { get; }
    }
}