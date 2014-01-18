using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface ISyncCommand : IAwaitableResponse<IMessage>
    {
        IMessage Request { get; }
    }
}