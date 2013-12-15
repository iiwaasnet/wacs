using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using wacs.Configuration;
using wacs.core;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.Paxos.Interface;
using wacs.Resolver.Interface;

namespace wacs.Resolver.Implementation
{
    public class HostResolver : IHostResolver
    {
        private readonly IMessageHub messageHub;
        private volatile INode localNode;
        private readonly ConcurrentDictionary<INode, string> processMap;
        private readonly string localEndpoint;
        private readonly ILogger logger;
        private readonly IListener listener;
        private readonly CancellationTokenSource cancellation;
        private readonly Task worldLearningTask;
        private readonly ISynodConfigurationProvider configProvider;

        public HostResolver(IMessageHub messageHub,
                            ISynodConfigurationProvider configProvider,
                            IHostResolverConfiguration config,
                            ILogger logger)
        {
            this.messageHub = messageHub;
            this.logger = logger;
            processMap = new ConcurrentDictionary<INode, string>();
            localEndpoint = configProvider.Synod.GetLocalEndpoint();
            localNode = configProvider.LocalNode;
            configProvider.WorldChanged += OnWorldChanged;
            this.configProvider = configProvider;

            cancellation = new CancellationTokenSource();

            listener = messageHub.Subscribe();
            worldLearningTask = new Task(() => ResolveWorld(cancellation.Token, config.ProcessIdBroadcastPeriod));
        }

        private void OnWorldChanged()
        {
            var world = configProvider.World.Select(w => w.Address);

            var deadNodes = processMap
                .Where(node => !world.Contains(node.Value))
                .Aggregate(Enumerable.Empty<INode>(), (current, node) => current.Concat(new[] {node.Key}));

            var val = string.Empty;
            foreach (var deadNode in deadNodes)
            {
                processMap.TryRemove(deadNode, out val);
            }
        }

        public void Start()
        {
            worldLearningTask.Start();
        }

        public void Stop()
        {
            cancellation.Cancel(false);
            worldLearningTask.Wait();
            worldLearningTask.Dispose();
        }

        private void ResolveWorld(CancellationToken token, TimeSpan processIdBroadcastPeriod)
        {
            try
            {
                listener.Start();
                using (listener.Subscribe(new MessageStreamListener(OnMessage)))
                {
                    while (!token.IsCancellationRequested)
                    {
                        messageHub.Broadcast(new ProcessAnnouncementMessage(localNode,
                                                                            new ProcessAnnouncementMessage.Payload
                                                                            {
                                                                                Endpoint = localEndpoint,
                                                                                ProcessId = localNode.Id
                                                                            }));
                        Thread.Sleep(processIdBroadcastPeriod);
                    }
                }
                listener.Stop();
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private void OnMessage(IMessage message)
        {
            if (message.Body.MessageType == ProcessAnnouncementMessage.MessageType)
            {
                var senderEndpoint = new ProcessAnnouncementMessage(message).GetPayload().Endpoint;

                string endpoint;
                var process = message.Envelope.Sender;

                if (processMap.TryGetValue(process, out endpoint) && senderEndpoint != endpoint)
                {
                    Console.WriteLine("Conflicting processes! Existing {0}@{1}, joining {2}@{3}",
                                      process.Id,
                                      endpoint,
                                      process.Id,
                                      senderEndpoint);
                }
                else
                {
                    processMap[process] = senderEndpoint;
                }
            }
        }

        public IEnumerable<INode> GetWorld()
        {
            return processMap.Keys;
        }

        public INode GetLocalProcess()
        {
            return localNode;
        }
    }
}