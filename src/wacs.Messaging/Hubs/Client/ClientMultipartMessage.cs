using System;
using System.Collections.Generic;
using System.Linq;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using ZeroMQ;

namespace wacs.Messaging.Hubs.Client
{
    internal class ClientMultipartMessage
    {
        private readonly IEnumerable<byte[]> frames;

        public ClientMultipartMessage(IMessage message)
        {
            frames = BuildMessageParts(message).ToArray();
        }

        public ClientMultipartMessage(ZmqMessage message)
        {
            AssertMessage(message);

            frames = SplitMessageToFrames(message);
        }

        private IEnumerable<byte[]> SplitMessageToFrames(IEnumerable<Frame> message)
        {
            return message.Select(m => m.Buffer).ToArray();
        }

        private IEnumerable<byte[]> BuildMessageParts(IMessage message)
        {
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

        private static void AssertMessage(ZmqMessage message)
        {
            if (message.FrameCount < 3)
            {
                throw new Exception(string.Format("Inconsistent message received! FrameCount: [{0}] Bytes: [{1}]", message.FrameCount, message.TotalSize));
            }
        }

        internal int GetSenderId()
        {
            return frames.First().GetInt();
        }

        internal byte[] GetSenderIdBytes()
        {
            return frames.First();
        }

        internal string GetMessageType()
        {
            return frames.Skip(1).First().GetString();
        }

        internal byte[] GetMessageTypeBytes()
        {
            return frames.Skip(1).First();
        }

        internal byte[] GetMessage()
        {
            return frames.Skip(2).Aggregate(new byte[0], (seed, array) => seed.Concat(array).ToArray());
        }

        internal IEnumerable<byte[]> Frames
        {
            get { return frames; }
        }
    }
}