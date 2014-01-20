namespace wacs.Messaging.Messages
{
    public interface IMessage
    {
        Envelope Envelope { get; }
        Body Body { get; }
    }
}