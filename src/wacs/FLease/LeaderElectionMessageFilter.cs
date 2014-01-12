using System;
using System.Collections.Concurrent;
using wacs.Configuration;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Lease;
using wacs.Resolver;

namespace wacs.FLease
{
    public class LeaderElectionMessageFilter
    {
        private readonly IBallot ballot;
        private readonly string messageType;
        private readonly INodeResolver nodeResolver;
        private readonly Func<IMessage, ILeaseMessagePayload> payload;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;

        public LeaderElectionMessageFilter(IBallot ballot,
                                           string messageType,
                                           Func<IMessage, ILeaseMessagePayload> payload,
                                           INodeResolver nodeResolver,
                                           ISynodConfigurationProvider synodConfigurationProvider)
        {
            this.messageType = messageType;
            this.ballot = ballot;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.payload = payload;
            this.nodeResolver = nodeResolver;
        }

        public bool Match(IMessage message)
        {
            var process = new Process(message.Envelope.Sender.Id);

            if (ProcessIsInSynod(process))
            {
                if (message.Body.MessageType == messageType)
                {
                    var messagePayload = payload(message);

                    return messagePayload.Ballot.ProcessId == ballot.Process.Id
                           && messagePayload.Ballot.Timestamp == ballot.Timestamp.Ticks
                           && messagePayload.Ballot.MessageNumber == ballot.MessageNumber;
                }
            }

            return false;
        }

        private bool ProcessIsInSynod(IProcess process)
        {
            var node = nodeResolver.ResolveRemoteProcess(process);

            return node != null && synodConfigurationProvider.IsMemberOfSynod(node);
        }
    }
}