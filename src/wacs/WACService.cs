using System;
using Topshelf;
using wacs.Diagnostics;

namespace wacs
{
    public class WACService : ServiceControl
    {
        private readonly IBootstrapper bootstrapper;
        private readonly ILogger logger;

        public WACService(IBootstrapper bootstrapper, ILogger logger)
        {
            this.bootstrapper = bootstrapper;
            this.logger = logger;
        }

        public bool Start(HostControl hostControl)
        {
            bootstrapper.Start();

            logger.InfoFormat("WACS Id:[{0}] started at [{1}]", bootstrapper.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            bootstrapper.Stop();

            logger.InfoFormat("WACS Id:[{0}] stopped", bootstrapper.Id);

            return true;
        }
    }
}