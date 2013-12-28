using System.Collections.Generic;

namespace wacs.Configuration
{
    public interface ISynodConfiguration
    {
        IEndpoint LocalNode { get; }

        IEnumerable<IEndpoint> Members { get; }
    }
}