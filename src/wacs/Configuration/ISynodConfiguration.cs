using System.Collections.Generic;

namespace wacs.Configuration
{
    public interface ISynodConfiguration
    {
        IEnumerable<INode> Nodes { get; }
    }
}