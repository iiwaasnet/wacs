using System;
using Topshelf;
using wacs.Diagnostics;

namespace wacs
{
    public class WACService : ServiceControl
    {
        private readonly ITestService testService;
        private readonly ILogger logger;

        public WACService(ITestService testService, ILogger logger)
        {
            this.testService = testService;
            this.logger = logger;
        }

        public bool Start(HostControl hostControl)
        {
            testService.Start();

            logger.InfoFormat("WACS Id:[{0}] started at [{1}]", testService.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            testService.Stop();

            logger.InfoFormat("WACS Id:[{0}] stopped", testService.Id);

            return true;
        }
    }
}