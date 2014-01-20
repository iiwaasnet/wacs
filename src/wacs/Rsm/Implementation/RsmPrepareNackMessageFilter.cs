using System;
using wacs.Configuration;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class RsmPrepareNackMessageFilter
    {
        private readonly ILogIndex index;
        private readonly IBallot ballot;
        private readonly INodeResolver nodeResolver;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;

        public RsmPrepareNackMessageFilter(IBallot ballot,
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
            if (RsmNackPrepareBlocked.MessageType == message.Body.MessageType)
            {
                return new RsmNackPrepareBlocked(message).GetPayload();
            }
            if (RsmNackPrepareChosen.MessageType == message.Body.MessageType)
            {
                return new RsmNackPrepareChosen(message).GetPayload();
            }
            if (RsmNackPrepareNotLeader.MessageType == message.Body.MessageType)
            {
                return new RsmNackPrepareNotLeader(message).GetPayload();
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