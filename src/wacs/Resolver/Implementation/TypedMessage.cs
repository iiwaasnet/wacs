using wacs.Messaging;

namespace wacs.Resolver.Implementation
{
    //TODO: Move to more generic namespace, hence it's a base class for all messages
    public class TypedMessage<T> : Message, ITypedMessage<T>
        where T : class
    {
        private T payload;

        protected TypedMessage(IMessage message)
            : base(message.Envelope, message.Body)
        {
        }

        protected TypedMessage(IProcess sender, T payload)
        {
            Envelope = new Envelope {Sender = sender};
            Body = new Body
                   {
                       MessageType = MessageType,
                       Content = Serialize(payload)
                   };
        }

        public T GetPayload()
        {
            return payload ?? (payload = Deserialize<T>(Body.Content));
        }

        public static string MessageType { get; protected set; }
    }
}