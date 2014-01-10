using System;
using System.Threading;
using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Rsm.Implementation
{
    internal class AwaitableRsmResponse : IAwaitableResult<IMessage>
    {
        private readonly ManualResetEventSlim waitHandle;
        private IMessage response;
        
        internal AwaitableRsmResponse(IMessage command)
        {
            waitHandle = new ManualResetEventSlim(false);
            Command = command;
        }

        internal void SetResponse(IMessage response)
        {
            Interlocked.Exchange(ref this.response, response);

            waitHandle.Set();
        }

        void IDisposable.Dispose()
        {
            waitHandle.Dispose();
        }

        IMessage IAwaitableResult<IMessage>.GetResult()
        {
            waitHandle.Wait();

            return response;
        }

        IMessage IAwaitableResult<IMessage>.GetResult(TimeSpan timeout)
        {
            waitHandle.Wait(timeout);

            return response;
        }

        internal IMessage Command { get; private set; }
    }
}