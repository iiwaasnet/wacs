using Topshelf;
using wacs.Diagnostics;

namespace wacs
{
    public class WACService : ServiceControl
    {
        private readonly IStateMachine stateMachine;
        private readonly ILogger logger;

        public WACService(IStateMachine stateMachine, ILogger logger)
        {
            this.stateMachine = stateMachine;
            this.logger = logger;
        }

        public bool Start(HostControl hostControl)
        {
            stateMachine.Start();

            logger.InfoFormat("WACS Id:[{0}] started", stateMachine.Id);

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            stateMachine.Stop();

            logger.InfoFormat("WACS Id:[{0}] stopped", stateMachine.Id);

            return true;
        }
    }
}