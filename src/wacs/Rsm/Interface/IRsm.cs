using System;
using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IRsm : IDisposable
    {
        IAwaitableResponse<IMessage> EnqueueForExecution(IMessage command);
    }
}