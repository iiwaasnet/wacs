using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    public interface IClientMessageRouter
    {
        IMessage ForwardClientRequestToLeader(IMessage message);
    }
}