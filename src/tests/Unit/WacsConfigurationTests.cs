using NUnit.Framework;
using SimpleConfigSections;
using wacs.Configuration;

namespace tests.Unit
{
    [TestFixture]
    public class WacsConfigurationTests
    {
        [Test]
        public void LoadConfiguration()
        {
            var config = Configuration.Get<IWacsConfiguration>();
        }
    }
}