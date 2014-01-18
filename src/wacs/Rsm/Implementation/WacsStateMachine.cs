using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class WacsStateMachine : IReplicatedStateMachine
    {
        public void ProcessCommand(ISyncCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}