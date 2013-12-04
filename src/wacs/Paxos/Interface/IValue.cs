namespace wacs.Paxos.Interface
{
    public interface IValue
    {
        ICommand Command { get; }
    }
}