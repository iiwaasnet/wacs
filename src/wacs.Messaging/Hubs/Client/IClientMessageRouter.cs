using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    public interface IClientMessageRouter
    {
        IMessage ForwardClientRequestToLeader(INode leader, IMessage message);
    }
}