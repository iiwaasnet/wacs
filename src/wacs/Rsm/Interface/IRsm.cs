using System;
using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IRsm : IDisposable
    {
        IAwaitableResult<IMessage> EnqueueForExecution(IMessage command); 
    }
}