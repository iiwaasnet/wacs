namespace wacs.Messaging.Messages
{
    public interface IMessage
    {
        IEnvelope Envelope { get; }
        IBody Body { get; }
    }
}