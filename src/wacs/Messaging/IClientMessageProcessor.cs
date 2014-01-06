namespace wacs.Messaging
{
    public interface IClientMessageProcessor
    {
        IMessage ProcessClientMessage(IMessage message);
    }
}