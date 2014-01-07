using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    public interface IClientMessageProcessor
    {
        IMessage ProcessClientMessage(IMessage message);
    }
}