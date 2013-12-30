﻿using System;
using System.Collections.Concurrent;
using wacs.Configuration;
using wacs.FLease.Messages;
using wacs.Messaging;
using wacs.Paxos.Interface;
using wacs.Resolver.Interface;

namespace wacs.FLease
{
    public class LeaderElectionMessageFilter
    {
        private readonly ConcurrentDictionary<IProcess, object> responses;
        private readonly IBallot ballot;
        private readonly string messageType;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly Func<IMessage, ILeaseMessagePayload> payload;
        private readonly INodeResolver nodeResolver;

        public LeaderElectionMessageFilter(IBallot ballot,
                                           string messageType,
                                           Func<IMessage, ILeaseMessagePayload> payload,
                                           INodeResolver nodeResolver,
                                           ISynodConfigurationProvider synodConfigurationProvider)
        {
            responses = new ConcurrentDictionary<IProcess, object>();
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
                           && responses.TryAdd(process, null);
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