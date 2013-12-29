using System.Collections.Concurrent;
using System.Linq;
using wacs.Configuration;
using wacs.FLease.Messages;
using wacs.Messaging;
using wacs.Resolver.Interface;

namespace wacs.FLease
{
    public class LeaderElectionMessageFilter<TPayload>
        where TPayload : IMessagePayload
    {
        private readonly ConcurrentDictionary<IProcess, object> responses;
        private readonly IBallot ballot;
        private readonly FLeaseMessageType messageType;
        private readonly ISynod synod;
        private readonly IMessageSerializer serializer;
        private readonly INodeResolver nodeResolver;

        public LeaderElectionMessageFilter(IBallot ballot,
                                           FLeaseMessageType messageType,
                                           IMessageSerializer serializer,
                                           INodeResolver nodeResolver,
                                           ISynod synod)
        {
            responses = new ConcurrentDictionary<IProcess, object>();
            this.messageType = messageType;
            this.ballot = ballot;
            this.synod = synod;
            this.serializer = serializer;
            this.nodeResolver = nodeResolver;
        }

        public bool Match(IMessage message)
        {
            var process = new Process(message.Envelope.Sender.Id);

            if (ProcessIsInSynod(process))
            {
                if (message.Body.MessageType.ToMessageType() == messageType)
                {
                    var ackRead = serializer.Deserialize<TPayload>(message.Body.Content);

                    return ackRead.Ballot.ProcessId == ballot.Process.Id
                           && ackRead.Ballot.Timestamp == ballot.Timestamp.Ticks
                           && responses.TryAdd(process, null);
                }
            }

            return false;
        }

        private bool ProcessIsInSynod(IProcess process)
        {
            var node = nodeResolver.ResolveRemoteProcess(process);

            return node != null && synod.Members.Contains(node);
        }
    }
}