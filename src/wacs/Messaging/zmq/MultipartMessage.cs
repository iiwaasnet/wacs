using System;
using System.Collections.Generic;
using System.Linq;

namespace wacs.Messaging.zmq
{
    internal class MultipartMessage
    {
        private readonly IEnumerable<byte[]> parts;
        internal static readonly byte[] MulticastId;

        static MultipartMessage()
        {
            MulticastId = "*".GetBytes();
        }

        public MultipartMessage(IProcess recipient, IMessage message)
        {
            parts = BuildMessageParts(recipient, message).ToArray();
        }

        private IEnumerable<byte[]> BuildMessageParts(IProcess recipient, IMessage message)
        {
            yield return BuildMessageFilter(recipient);
            yield return BuildSenderId(message);
            yield return BuildMessageType(message);
            yield return BuildMessageBody(message);
        }

        private byte[] BuildMessageBody(IMessage message)
        {
            return message.Body.Content;
        }

        private byte[] BuildMessageType(IMessage message)
        {
            return message.Body.MessageType.GetBytes();
        }

        private byte[] BuildSenderId(IMessage message)
        {
            return message.Envelope.Sender.Id.GetBytes();
        }

        private byte[] BuildMessageFilter(IProcess recipient)
        {
            return (recipient != null)
                       ? recipient.Id.GetBytes()
                       : MulticastId;
        }

        public MultipartMessage(IEnumerable<byte[]> message)
        {
            AssertMessage(message);

            parts = message.ToArray();
        }

        private static void AssertMessage(IEnumerable<byte[]> message)
        {
            if (message.Count() < 4)
            {
                throw new Exception(string.Format("Inconsistent message received: [{0}]", string.Join("|", message)));
            }
        }

        internal string GetFilter()
        {
            return parts.First().GetString();
        }
        
        internal byte[] GetFilterBytes()
        {
            return parts.First();
        }

        internal int GetSenderId()
        {
            return parts.Skip(1).First().GetInt();
        }
        internal byte[] GetSenderIdBytes()
        {
            return parts.Skip(1).First();
        }

        internal string GetMessageType()
        {
            return parts.Skip(2).First().GetString();
        }
        internal byte[] GetMessageTypeBytes()
        {
            return parts.Skip(2).First();
        }

        internal byte[] GetMessage()
        {
            return parts.Skip(3).Aggregate(new byte[0], (seed, array) => seed.Concat(array).ToArray());
        }

        internal IEnumerable<byte[]> Parts
        {
            get { return parts; }
        }
    }
}