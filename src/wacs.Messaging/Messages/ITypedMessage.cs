namespace wacs.Messaging.Messages
{
    public interface ITypedMessage<out T> : IMessage
    {
        T GetPayload();
    }
}