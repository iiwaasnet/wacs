using wacs.Messaging;

namespace wacs.Resolver.Implementation
{
    public class TypedMessage<T> : Message, ITypedMessage<T>
        where T : class
    {
        private T payload;

        protected TypedMessage(IMessage message)
            : base(message.Envelope, message.Body)
        {
        }

        protected TypedMessage(IProcess sender, T payload, string messageType)
        {
            Envelope = new Envelope {Sender = sender};
            Body = new Body
                   {
                       MessageType = messageType,
                       Content = Serialize(payload)
                   };
        }

        public T GetPayload()
        {
            return payload ?? (payload = Deserialize<T>(Body.Content));
        }
    }
}