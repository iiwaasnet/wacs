using System.Collections.Generic;
using wacs.Configuration;

namespace tests.Unit.Helpers
{
    internal class SynodConfiguration : ISynodConfiguration
    {
        public IEndpoint LocalNode { get; set; }
        public IEnumerable<IEndpoint> Members { get; set; }
    }
}