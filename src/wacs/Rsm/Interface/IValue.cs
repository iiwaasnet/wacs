using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IValue
    {
        IMessage Command { get; }
    }
}