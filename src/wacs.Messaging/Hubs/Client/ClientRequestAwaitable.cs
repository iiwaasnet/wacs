using System;
using System.Threading;
using wacs.Configuration;
using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    internal class ClientRequestAwaitable : IAwaitableResult<IMessage>
    {
        private readonly ManualResetEventSlim waitHandle;
        private IMessage response;

        public ClientRequestAwaitable(INode leader, IMessage request)
        {
            Request = request;
            Leader = leader;
            waitHandle = new ManualResetEventSlim(false);
        }

        internal void SetResponse(IMessage response)
        {
            Interlocked.Exchange(ref response, response);
            waitHandle.Set();
        }

        IMessage IAwaitableResult<IMessage>.GetResult()
        {
            waitHandle.Wait();

            return response;
        }

        IMessage IAwaitableResult<IMessage>.GetResult(TimeSpan timeout)
        {
            if (waitHandle.Wait(timeout))
            {
                return response;
            }

            throw new TimeoutException();
        }

        public void Dispose()
        {
            waitHandle.Dispose();
        }

        internal IMessage Request { get; private set; }
        internal INode Leader { get; private set; }
    }
}