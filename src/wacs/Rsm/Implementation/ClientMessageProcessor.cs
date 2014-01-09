using System;
using wacs.FLease;
using wacs.Messaging.Hubs.Client;
using wacs.Messaging.Messages;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ClientMessageProcessor : IClientMessageProcessor
    {
        private readonly IClientMessageRouter messageRouter;
        private readonly ILeaseProvider leaseProvider;
        private readonly INodeResolver nodeResolver;

        public ClientMessageProcessor(IClientMessageHub clientMessageHub,
                                      IClientMessageRouter messageRouter,
                                      INodeResolver nodeResolver,
                                      ILeaseProvider leaseProvider)
        {
            this.messageRouter = messageRouter;
            this.leaseProvider = leaseProvider;
            this.nodeResolver = nodeResolver;
            clientMessageHub.RegisterMessageProcessor(ProcessClientMessage);
        }

        private IMessage ProcessClientMessage(IMessage request)
        {
            var lease = GetCurrentLease();

            if (MessageRequiresLeadership(request) && !LocalNodeIsLeader(lease))
            {
                return messageRouter.ForwardClientRequestToLeader(nodeResolver.ResolveRemoteProcess(lease.Owner), request);
            }

            return HandleClientMessageAtLocalNode(request);
        }

        private IMessage HandleClientMessageAtLocalNode(IMessage request)
        {
            throw new NotImplementedException();
        }

        private bool MessageRequiresLeadership(IMessage message)
        {
            return messageRouter.MessageRequiresLidership(message);
        }

        private bool LocalNodeIsLeader(ILease lease)
        {
            return lease.Owner.Equals(nodeResolver.ResolveLocalNode());
        }

        private ILease GetCurrentLease()
        {
            // TODO: set timeout for getting lease

            var lease = leaseProvider.GetLease().Result;

            if (lease == null)
            {
                throw new TimeoutException("Timed out getting the lease!");
            }

            return lease;
        }
    }
}