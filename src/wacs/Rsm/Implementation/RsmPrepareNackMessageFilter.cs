﻿using wacs.Configuration;
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
            var payload = (RsmNackPrepareBlocked.MessageType == message.Body.MessageType)
                                          ? (IPreparePayload) new RsmNackPrepareBlocked(message).GetPayload()
                                          : (IPreparePayload) new RsmNackPrepareChosen(message).GetPayload();

            var process = new Process(message.Envelope.Sender.Id);

            return ProcessIsInSynod(process)
                   && payload.PrepareBallot.Equals(ballot)
                   && payload.LogIndex.Equals(index);
        }

        private bool ProcessIsInSynod(IProcess process)
        {
            var node = nodeResolver.ResolveRemoteProcess(process);

            return node != null && synodConfigurationProvider.IsMemberOfSynod(node);
        }
    }
}