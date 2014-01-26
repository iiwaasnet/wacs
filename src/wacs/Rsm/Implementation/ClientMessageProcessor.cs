using System;
using wacs.Communication.Hubs.Client;
using wacs.Configuration;
using wacs.FLease;
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
        private readonly IRsm rsm;
        private readonly IClientMessagesRepository messagesRepository;
        private readonly IRsmConfiguration rsmConfiguration;

        public ClientMessageProcessor(IClientMessageHub clientMessageHub,
                                      IClientMessageRouter messageRouter,
                                      INodeResolver nodeResolver,
                                      IClientMessagesRepository messagesRepository,
                                      IRsm rsm,
                                      ILeaseProvider leaseProvider,
                                      IRsmConfiguration rsmConfiguration)
        {
            this.messageRouter = messageRouter;
            this.leaseProvider = leaseProvider;
            this.nodeResolver = nodeResolver;
            this.rsm = rsm;
            this.messagesRepository = messagesRepository;
            this.rsmConfiguration = rsmConfiguration;
            clientMessageHub.RegisterMessageProcessor(ProcessClientMessage);
        }

        private IMessage ProcessClientMessage(IMessage request)
        {
            var lease = GetCurrentLease();

            if (MessageRequiresQuorum(request) && !LocalNodeIsLeader(lease))
            {
                return messageRouter.ForwardClientRequestToLeader(nodeResolver.ResolveRemoteProcess(lease.Owner), request);
            }

            return HandleClientMessageAtLocalNode(request);
        }

        private IMessage HandleClientMessageAtLocalNode(IMessage request)
        {
            if (MessageRequiresQuorum(request))
            {
                var awaitable = rsm.EnqueueForExecution(request);

                return awaitable.GetResponse(rsmConfiguration.CommandExecutionTimeout);
            }
            // TODO: Add to a lsmCommandQueue (Local State Machine queue)
            return null;
        }

        private bool MessageRequiresQuorum(IMessage message)
        {
            return messagesRepository.RequiresQuorum(message);
        }

        private bool LocalNodeIsLeader(ILease lease)
        {
            return lease.Owner.Equals(nodeResolver.ResolveLocalNode());
        }

        private ILease GetCurrentLease()
        {
            // TODO: set timeout for getting lease

            var lease = leaseProvider.GetLease();

            if (lease == null)
            {
                throw new TimeoutException("Timed out getting the lease!");
            }

            return lease;
        }
    }
}