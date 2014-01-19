using System;
using System.Threading;
using wacs.Configuration;
using wacs.Framework.State;
using wacs.Messaging.Messages;

namespace wacs.Communication.Hubs.Client
{
    internal class AwaitableResponse : IAwaitableResponse<IMessage>
    {
        private readonly ManualResetEventSlim waitHandle;
        private IMessage response;

        public AwaitableResponse(INode leader, IMessage request)
        {
            Request = request;
            Leader = leader;
            waitHandle = new ManualResetEventSlim(false);
        }

        internal void SetResponse(IMessage response)
        {
            Interlocked.Exchange(ref this.response, response);
            waitHandle.Set();
        }

        IMessage IAwaitableResponse<IMessage>.GetResponse()
        {
            waitHandle.Wait();

            return response;
        }

        IMessage IAwaitableResponse<IMessage>.GetResponse(TimeSpan timeout)
        {
            if (waitHandle.Wait(timeout))
            {
                return response;
            }

            throw new TimeoutException();
        }

        void IDisposable.Dispose()
        {
            waitHandle.Dispose();
        }

        internal IMessage Request { get; private set; }
        internal INode Leader { get; private set; }
    }
}