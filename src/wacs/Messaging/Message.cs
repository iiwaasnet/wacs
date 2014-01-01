using wacs.FLease;

namespace wacs.Messaging
{
    public class Message : IMessage
	{
        private static readonly IMessageSerializer messageSerializer;

        static Message()
        {
            messageSerializer = new MessageSerializer();
        }

        protected Message()
        {
        }

        public Message(IEnvelope envelope, IBody body)
        {
            Envelope = envelope;
            Body = body;
        }

        protected byte[] Serialize(object payload)
        {
            return messageSerializer.Serialize(payload);
        }

        static protected T Deserialize<T>(byte[] content)
        {
            return messageSerializer.Deserialize<T>(content);
        }

        public IEnvelope Envelope { get; protected set; }
		public IBody Body { get; protected set; }

	}
}