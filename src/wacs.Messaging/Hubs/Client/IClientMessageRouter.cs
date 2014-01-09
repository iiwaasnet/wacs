using System;
using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    public interface IClientMessageRouter : IDisposable
    {
        IMessage ForwardClientRequestToLeader(INode leader, IMessage message);
        bool MessageRequiresLidership(IMessage message);
    }
}