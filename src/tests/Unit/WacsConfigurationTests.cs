using NUnit.Framework;
using wacs.Configuration;

namespace tests.Unit
{
    [TestFixture]
    public class WacsConfigurationTests
    {
        [Test]
        public void TestLoadConfiguration()
        {
            var config = SimpleConfigSections.Configuration.Get<IWacsConfiguration>();
        }
    }
}