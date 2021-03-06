﻿using System;
using wacs.Configuration;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class RsmMessageFilter
    {
        private readonly ILogIndex index;
        private readonly IBallot ballot;
        private readonly INodeResolver nodeResolver;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly Func<IMessage, IConsensusDecisionPayload> payload;

        public RsmMessageFilter(IBallot ballot,
                                ILogIndex index,
                                Func<IMessage, IConsensusDecisionPayload> payload,
                                INodeResolver nodeResolver,
                                ISynodConfigurationProvider synodConfigurationProvider)
        {
            this.index = index;
            this.ballot = ballot;
            this.nodeResolver = nodeResolver;
            this.payload = payload;
            this.synodConfigurationProvider = synodConfigurationProvider;
        }

        internal bool Match(IMessage message)
        {
            var messagePayload = payload(message);
            var process = new Configuration.Process(message.Envelope.Sender.Id);

            return ProcessIsInSynod(process)
                   && new Ballot(messagePayload.Proposal.ProposalNumber).Equals(ballot)
                   && new LogIndex(messagePayload.LogIndex.Index).Equals(index);
        }

        private bool ProcessIsInSynod(IProcess process)
        {
            var node = nodeResolver.ResolveRemoteProcess(process);

            return node != null && synodConfigurationProvider.IsMemberOfSynod(node);
        }
    }
}