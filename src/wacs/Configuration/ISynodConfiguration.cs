using System.Collections.Generic;

namespace wacs.Configuration
{
    public interface ISynodConfiguration
    {
        INode This { get; }
        IEnumerable<INode> Nodes { get; }
    }
}