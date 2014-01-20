using System;
using wacs.Configuration;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class RsmAcceptNackMessageFilter
    {
        private readonly ILogIndex index;
        private readonly IBallot ballot;
        private readonly INodeResolver nodeResolver;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;

        public RsmAcceptNackMessageFilter(IBallot ballot,
                                           ILogIndex index,
                                           INodeResolver nodeResolver,
                                           ISynodConfigurationProvider synodConfigurationProvider)
        {
            this.index = index;
            this.ballot = ballot;
            this.nodeResolver = nodeResolver;
            this.synodConfigurationProvider = synodConfigurationProvider;
        }

        internal bool Match(IMessage message)
        {
            var payload = GetPayload(message);

            var process = new Configuration.Process(message.Envelope.Sender.Id);

            return ProcessIsInSynod(process)
                   && new Ballot(payload.Proposal.ProposalNumber).Equals(ballot)
                   && new LogIndex(payload.LogIndex.Index).Equals(index);
        }

        private static IConsensusDecisionPayload GetPayload(IMessage message)
        {
            if (RsmNackAcceptBlocked.MessageType == message.Body.MessageType)
            {
                return new RsmNackAcceptBlocked(message).GetPayload();
            }
            if (RsmNackAcceptChosen.MessageType == message.Body.MessageType)
            {
                return new RsmNackAcceptChosen(message).GetPayload();
            }
            if (RsmNackAcceptNotLeader.MessageType == message.Body.MessageType)
            {
                return new RsmNackAcceptNotLeader(message).GetPayload();
            }

            throw new Exception(string.Format("Message type {0} is unknown!", message.Body.MessageType));
        }

        private bool ProcessIsInSynod(IProcess process)
        {
            var node = nodeResolver.ResolveRemoteProcess(process);

            return node != null && synodConfigurationProvider.IsMemberOfSynod(node);
        }
    }
}