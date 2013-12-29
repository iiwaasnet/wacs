using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.Paxos.Implementation;
using wacs.Paxos.Interface;
using wacs.Resolver.Interface;

namespace wacs.Resolver.Implementation
{
    public class NodeResolver : INodeResolver
    {
        private readonly IMessageHub messageHub;
        private volatile IProcess localProcess;
        private readonly ConcurrentDictionary<INode, IProcess> nodeToProcessMap;
        private readonly ConcurrentDictionary<IProcess, INode> processToNodeMap;
        private readonly INode localNode;
        private readonly ILogger logger;
        private readonly IListener listener;
        private readonly CancellationTokenSource cancellation;
        private readonly Task worldLearningTask;
        private readonly ISynodConfigurationProvider synodConfigProvider;

        public NodeResolver(IMessageHub messageHub,
                            ISynodConfigurationProvider synodConfigProvider,
                            IHostResolverConfiguration config,
                            ILogger logger)
        {
            this.messageHub = messageHub;
            this.logger = logger;
            localNode = synodConfigProvider.LocalNode;
            localProcess = synodConfigProvider.LocalProcess;
            nodeToProcessMap = new ConcurrentDictionary<INode, IProcess>(new[] {new KeyValuePair<INode, IProcess>(localNode, localProcess)});
            processToNodeMap = new ConcurrentDictionary<IProcess, INode>(new[] {new KeyValuePair<IProcess, INode>(localProcess, localNode)});
            synodConfigProvider.WorldChanged += RemoveDeadNodes;
            this.synodConfigProvider = synodConfigProvider;

            cancellation = new CancellationTokenSource();

            listener = messageHub.Subscribe();
            worldLearningTask = new Task(() => ResolveWorld(cancellation.Token, config.ProcessIdBroadcastPeriod));
        }

        private void RemoveDeadNodes()
        {
            var deadNodes = nodeToProcessMap
                .Where(node => !synodConfigProvider.World.Contains(node.Key))
                .Aggregate(Enumerable.Empty<INode>(), (current, node) => current.Concat(new[] {node.Key}));

            deadNodes.ForEach(RemoveProcessMapping);
        }

        private void RemoveProcessMapping(INode deadNode)
        {
            IProcess val;
            if (nodeToProcessMap.TryRemove(deadNode, out val))
            {
                INode node;
                processToNodeMap.TryRemove(val, out node);
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
                        messageHub.Broadcast(new ProcessAnnouncementMessage(localProcess,
                                                                            new ProcessAnnouncementMessage.Payload
                                                                            {
                                                                                Endpoint = localNode.Address,
                                                                                ProcessId = localProcess.Id
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
                var announcement = new ProcessAnnouncementMessage(message).GetPayload();
                var announcedEndpoint = new Node(announcement.Endpoint);
                var joiningProcess = new Process(announcement.ProcessId);

                INode registeredNode;
                if (processToNodeMap.TryGetValue(joiningProcess, out registeredNode) && !announcedEndpoint.Equals(registeredNode))
                {
                    //TODO: Add to conflict list to be displayed on management console
                    logger.WarnFormat("Conflicting processes! Existing {0}@{1}, joining {2}@{3}",
                                      announcement.ProcessId,
                                      registeredNode,
                                      announcement.ProcessId,
                                      announcement.Endpoint);
                }
                else
                {
                    if (processToNodeMap.TryAdd(joiningProcess, announcedEndpoint))
                    {
                        nodeToProcessMap[announcedEndpoint] = joiningProcess;
                    }
                }
            }
        }

        public IProcess ResolveRemoteNode(INode node)
        {
            IProcess resolved;
            nodeToProcessMap.TryGetValue(node, out resolved);

            return resolved;
        }

        public INode ResolveRemoteProcess(IProcess process)
        {
            INode node;
            processToNodeMap.TryGetValue(process, out node);

            return node;
        }

        public IProcess ResolveLocalNode()
        {
            return localProcess;
        }
    }
}