using System;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    public interface IClientMessageHub : IDisposable
    {
        void RegisterMessageProcessor(Func<IMessage, IMessage> handler);
    }
}