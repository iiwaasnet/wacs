using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.Communication.Hubs.Intercom
{
    internal class IntercomMultipartMessage
    {
        private readonly IEnumerable<byte[]> frames;
        internal static readonly byte[] MulticastId;

        static IntercomMultipartMessage()
        {
            MulticastId = "*".GetBytes();
        }

        public IntercomMultipartMessage(IProcess recipient, IMessage message)
        {
            frames = BuildMessageParts(recipient, message).ToArray();
        }

        public IntercomMultipartMessage(NetMQMessage message)
        {
            AssertMessage(message);

            frames = SplitMessageToFrames(message);
        }

        private IEnumerable<byte[]> SplitMessageToFrames(IEnumerable<NetMQFrame> message)
        {
            return message.Select(m => m.Buffer).ToArray();
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

        private static void AssertMessage(NetMQMessage message)
        {
            if (message.FrameCount < 4)
            {
                throw new Exception($"Inconsistent message received! FrameCount: [{message.FrameCount}]");
            }
        }

        internal string GetFilter()
        {
            return frames.First().GetString();
        }

        internal byte[] GetFilterBytes()
        {
            return frames.First();
        }

        internal int GetSenderId()
        {
            return frames.Skip(1).First().GetInt();
        }

        internal byte[] GetSenderIdBytes()
        {
            return frames.Skip(1).First();
        }

        internal string GetMessageType()
        {
            return frames.Skip(2).First().GetString();
        }

        internal byte[] GetMessageTypeBytes()
        {
            return frames.Skip(2).First();
        }

        internal byte[] GetMessage()
        {
            return frames.Skip(3).Aggregate(new byte[0], (seed, array) => seed.Concat(array).ToArray());
        }

        internal IEnumerable<byte[]> Frames
        {
            get { return frames; }
        }
    }
}