using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using wacs.Configuration;
using wacs.core.State;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging;
using wacs.ResolutionService.Interface;

namespace wacs.ResolutionService.Implementation
{
    public class ResolutionService : IResolutionService
    {
        private readonly IMessageHub messageHub;
        private readonly IObservableCondition synodResolved;
        private volatile IProcess hostingProcess;
        private readonly IObservableConcurrentDictionary<string, IProcess> processMap;
        private readonly string localEndpoint;
        private readonly ILogger logger;

        public ResolutionService(IMessageHub messageHub, ISynodConfiguration config, ILogger logger)
        {
            this.messageHub = messageHub;
            this.logger = logger;
            processMap = new ObservableConcurrentDictionary<string, IProcess>();
            synodResolved = new ObservableCondition(() => SynodResolved(config.Nodes), new[] {processMap});
            localEndpoint = GetLocalEndpoint(config.Nodes);
            //hostingProcess = GenerateProcessId();
            hostingProcess = new Process(12);

            var listener = messageHub.Subscribe(hostingProcess);
            listener.Start();
            listener.Subscribe(new MessageStreamListener(OnMessage));

            Task.Factory.StartNew(() => ResolveSynod(config.Nodes));
        }

        private bool SynodResolved(IEnumerable<INode> nodes)
        {
            return processMap.Count() == nodes.Count();
        }

        private void ResolveSynod(IEnumerable<INode> nodes)
        {
            try
            {
                while (!SynodResolved(nodes))
                {
                    messageHub.Broadcast(new ProcessAnnouncementMessage(hostingProcess,
                                                                        new ProcessAnnouncementMessage.Payload
                                                                        {
                                                                            Endpoint = localEndpoint,
                                                                            ProcessId = hostingProcess.Id
                                                                        }));
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                }
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private static Process GenerateProcessId()
        {
            var rnd = new Random((int) (0x0000ffff & DateTime.UtcNow.Ticks));
            Thread.Sleep(TimeSpan.FromMilliseconds(rnd.Next(20, 100)));
            Thread.Sleep(TimeSpan.FromMilliseconds(rnd.Next(20, 100)));

            return new Process();
        }

        private string GetLocalEndpoint(IEnumerable<INode> nodes)
        {
            var endpoint = GetLocalConfiguredEndpoint(nodes);

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = GetLocalResolvedEndpoint(nodes);
            }

            return endpoint.TrimEnd('/');
        }

        private string GetLocalResolvedEndpoint(IEnumerable<INode> nodes)
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

        private string GetLocalConfiguredEndpoint(IEnumerable<INode> nodes)
        {
            var uri = nodes
                .Where(n => n.IsLocal)
                .Select(n => new Uri(n.Address).AbsoluteUri)
                .FirstOrDefault();

            return uri;
        }

        private void OnMessage(IMessage message)
        {
            if (message.Body.MessageType == ProcessAnnouncementMessage.MessageType)
            {
                var senderIp = new ProcessAnnouncementMessage(message).GetPayload().Endpoint;

                if (SenderIdCollidesWithLocal(message.Envelope.Sender, senderIp))
                {
                    Console.WriteLine("Restart process {0} at {1}", hostingProcess.Id, localEndpoint);
                    Interlocked.Exchange(ref hostingProcess, GenerateProcessId());
                }
                else
                {
                    processMap[senderIp] = message.Envelope.Sender;

                    if (senderIp != localEndpoint)
                    {
                        messageHub.Broadcast(new ProcessAnnouncementMessage(hostingProcess,
                                                                            new ProcessAnnouncementMessage.Payload
                                                                            {
                                                                                Endpoint = localEndpoint,
                                                                                ProcessId = hostingProcess.Id
                                                                            }));
                    }
                }
            }
        }

        private bool SenderIdCollidesWithLocal(IProcess sender, string senderIp)
        {
            return hostingProcess.Equals(sender) && senderIp != localEndpoint;
        }

        public Task<IEnumerable<IProcess>> GetWorld()
        {
            return Task.Factory.StartNew(() =>
                                         {
                                             synodResolved.Waitable.WaitOne();
                                             foreach (var keyValuePair in processMap)
                                             {
                                                 Console.WriteLine("{0} @ {1}", keyValuePair.Value.Id, keyValuePair.Key);
                                             }
                                             return processMap.Values;
                                         });
        }

        public Task<IProcess> GetLocalProcess()
        {
            return Task.Factory.StartNew(() =>
                                         {
                                             synodResolved.Waitable.WaitOne();
                                             return hostingProcess;
                                         });
        }
    }
}