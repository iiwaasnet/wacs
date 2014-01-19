using System;
using wacs.Messaging.Messages;

namespace wacs.Communication.Hubs.Client
{
    public interface IClientMessageHub : IDisposable
    {
        void RegisterMessageProcessor(Func<IMessage, IMessage> handler);
    }
}