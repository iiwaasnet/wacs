using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using wacs.Configuration;
using wacs.core;
using wacs.core.State;
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
        private readonly IObservableCondition synodResolved;
        private volatile INode localNode;
        private readonly IObservableConcurrentDictionary<INode, string> processMap;
        private readonly string localEndpoint;
        private readonly ILogger logger;
        private readonly IListener listener;
        private readonly CancellationTokenSource cancellation;
        private readonly Task worldLearningTask;

        public HostResolver(IMessageHub messageHub,
                            ISynodConfigurationProvider configProvider,
                            IHostResolverConfiguration config,
                            ILogger logger)
        {
            this.messageHub = messageHub;
            this.logger = logger;
            processMap = new ObservableConcurrentDictionary<INode, string>();
            synodResolved = new ObservableCondition(() => SynodResolved(configProvider.World), new[] {processMap});
            localEndpoint = configProvider.World.GetLocalEndpoint();
            localNode = configProvider.LocalNode;

            cancellation = new CancellationTokenSource();

            listener = messageHub.Subscribe();
            worldLearningTask = new Task(() => ResolveSynod(cancellation.Token, config.ProcessIdBroadcastPeriod));
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

        private bool SynodResolved(IEnumerable<Configuration.INode> nodes)
        {
            return processMap.Count() == nodes.Count();
        }

        private void ResolveSynod(CancellationToken token, TimeSpan processIdBroadcastPeriod)
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

        private string GetLocalResolvedEndpoint(IEnumerable<Configuration.INode> nodes)
        {
            var localIP = Dns.GetHostEntry(Dns.GetHostName())
                             .AddressList
                             .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                                   || ip.AddressFamily == AddressFamily.InterNetworkV6);

            if (localIP == null)
            {
                throw new Exception("Unable to resolve host external IP address!");
            }

            var uri = nodes
                .Select(n => new Uri(n.Address, UriKind.Absolute))
                .FirstOrDefault(n => n.Host == localIP.ToString());

            if (uri == null)
            {
                throw new Exception("Host is not configured to be part of the cluster!");
            }

            return uri.AbsoluteUri;
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

        public Task<IEnumerable<INode>> GetWorld()
        {
            return Task.Factory.StartNew(() =>
                                         {
                                             synodResolved.Waitable.WaitOne();
                                             foreach (var keyValuePair in processMap)
                                             {
                                                 Console.WriteLine("{0} @ {1}", keyValuePair.Key.Id, keyValuePair.Value);
                                             }
                                             return processMap.Keys;
                                         });
        }

        public Task<INode> GetLocalProcess()
        {
            return Task.Factory.StartNew(() =>
                                         {
                                             synodResolved.Waitable.WaitOne();
                                             return localNode;
                                         });
        }
    }
}