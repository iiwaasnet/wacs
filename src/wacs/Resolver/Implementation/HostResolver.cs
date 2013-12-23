using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
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
        private readonly ConcurrentDictionary<string, INode> uriToProcessMap;
        private readonly ConcurrentDictionary<INode, string> processToUriMap;
        private readonly string localEndpoint;
        private readonly ILogger logger;
        private readonly IListener listener;
        private readonly CancellationTokenSource cancellation;
        private readonly Task worldLearningTask;
        private readonly ISynodConfigurationProvider synodConfigProvider;

        public HostResolver(IMessageHub messageHub,
                            ISynodConfigurationProvider synodConfigProvider,
                            IHostResolverConfiguration config,
                            ILogger logger)
        {
            this.messageHub = messageHub;
            this.logger = logger;
            localEndpoint = synodConfigProvider.Synod.GetLocalEndpoint();
            localNode = synodConfigProvider.LocalNode;
            uriToProcessMap = new ConcurrentDictionary<string, INode>(new[] {new KeyValuePair<string, INode>(localEndpoint, localNode)});
            processToUriMap = new ConcurrentDictionary<INode, string>(new[] {new KeyValuePair<INode, string>(localNode, localEndpoint)});
            synodConfigProvider.WorldChanged += RemoveDeadNodes;
            this.synodConfigProvider = synodConfigProvider;

            cancellation = new CancellationTokenSource();

            listener = messageHub.Subscribe();
            worldLearningTask = new Task(() => ResolveWorld(cancellation.Token, config.ProcessIdBroadcastPeriod));
        }

        private void RemoveDeadNodes()
        {
            var world = synodConfigProvider.World.Select(w => w.Address);

            var deadNodes = uriToProcessMap
                .Where(node => !world.Contains(node.Key))
                .Aggregate(Enumerable.Empty<string>(), (current, node) => current.Concat(new[] {node.Key}));

            deadNodes.ForEach(RemoveProcessMapping);
        }

        private void RemoveProcessMapping(string deadNodeUri)
        {
            INode val;
            if (uriToProcessMap.TryRemove(deadNodeUri, out val))
            {
                string uri;
                processToUriMap.TryRemove(val, out uri);
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
                var announcement = new ProcessAnnouncementMessage(message).GetPayload();
                var joiningProcess = new Node(announcement.ProcessId);

                string registeredUri;
                if(processToUriMap.TryGetValue(joiningProcess, out registeredUri) && announcement.Endpoint != registeredUri)
                {
                    //TODO: Add to conflict list to be displayed on management console
                    logger.WarnFormat("Conflicting processes! Existing {0}@{1}, joining {2}@{3}",
                                      announcement.ProcessId,
                                      registeredUri,
                                      announcement.ProcessId,
                                      announcement.Endpoint);
                }
                else
                {
                    if (processToUriMap.TryAdd(joiningProcess, announcement.Endpoint))
                    {
                        uriToProcessMap[announcement.Endpoint] = joiningProcess;
                    }
                }
            }
        }

        
        public INode ResolveRemoteProcess(Configuration.INode node)
        {
            INode resolved;
            uriToProcessMap.TryGetValue(node.Address, out resolved);

            return resolved;
        }

        public INode ResolveLocalProcess()
        {
            return localNode;
        }
    }
}