namespace wacs.Messaging.Messages
{
    public interface IClientMessagesRepository
    {
        bool RequiresQuorum(IMessage message);
    }
}