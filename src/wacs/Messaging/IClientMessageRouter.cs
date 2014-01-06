namespace wacs.Messaging
{
    public interface IClientMessageRouter
    {
        IMessage ForwardClientRequestToLeader(IMessage message);
    }
}