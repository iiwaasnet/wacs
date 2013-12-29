using System.Collections.Generic;

namespace wacs.Configuration
{
    public interface ISynod
    {
        string Id { get; }
        IEnumerable<INode> Members { get; }
    }
}