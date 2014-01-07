using System.Collections.Generic;

namespace wacs.Configuration
{
    public class Synod : ISynod
    {
        public string Id { get; set; }
        public IEnumerable<INode> Members { get; set; }
    }
}