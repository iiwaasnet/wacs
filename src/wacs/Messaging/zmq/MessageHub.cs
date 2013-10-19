using ZMQ;

namespace wacs.Messaging.zmq
{
    public class MessageHub : IMessageHub
    {
        private readonly Context context;
        private readonly Socket listener;
        private readonly Socket sender;

        public MessageHub()
        {
            context = new Context();
            listener = context.Socket(SocketType.SUB);
            sender = context.Socket(SocketType.PUB);
        }

        public void Dispose()
        {
            listener.Dispose();
            sender.Dispose();
            context.Dispose();
        }

        public IListener Subscribe(IProcess subscriber)
        {
            throw new System.NotImplementedException();
        }

        public void Broadcast(IMessage message)
        {
            throw new System.NotImplementedException();
        }

        public void Send(IProcess recipient, IMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}