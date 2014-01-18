using System;
using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IRsm : IDisposable
    {
        IAwaitableResponse<Messaging.Messages.IMessage> EnqueueForExecution(Messaging.Messages.IMessage command); 
    }
}