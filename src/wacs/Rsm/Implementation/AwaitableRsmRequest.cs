using System;
using System.Threading;
using wacs.Framework.State;
using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    internal class AwaitableRsmRequest : ISyncCommand
    {
        private readonly ManualResetEventSlim waitHandle;
        private IMessage response;

        internal AwaitableRsmRequest(IMessage command)
        {
            waitHandle = new ManualResetEventSlim(false);
            Request = command;
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

        IMessage IAwaitableResponse<IMessage>.GetResponse()
        {
            waitHandle.Wait();

            return response;
        }

        IMessage IAwaitableResponse<IMessage>.GetResponse(TimeSpan timeout)
        {
            waitHandle.Wait(timeout);

            return response;
        }

        public IMessage Request { get; set; }
    }
}