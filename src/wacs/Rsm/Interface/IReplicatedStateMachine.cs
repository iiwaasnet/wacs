namespace wacs.Rsm.Interface
{
    public interface IReplicatedStateMachine
    {
        void ProcessCommand(ISyncCommand command);
    }
}