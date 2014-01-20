using wacs.Diagnostics;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs
{
    public class Bootstrapper : IBootstrapper
    {
        private readonly ILogger logger;
        private readonly IClientMessageProcessor clientMessageProcessor;
        private readonly INodeResolver nodeResolver;
        private readonly IAcceptor acceptor;
        private readonly IReplicationDemultiplexor replicationDemultiplexor;

        public Bootstrapper(INodeResolver nodeResolver,
                            IClientMessageProcessor clientMessageProcessor,
                            IReplicationDemultiplexor replicationDemultiplexor,
                            IAcceptor acceptor,
                            ILogger logger)
        {
            this.logger = logger;
            this.nodeResolver = nodeResolver;
            this.acceptor = acceptor;
            this.clientMessageProcessor = clientMessageProcessor;
            this.replicationDemultiplexor = replicationDemultiplexor;
        }

        //private void ApplyCommands(CancellationToken token)
        //{
        //    var timer = new Stopwatch();
        //    var rnd = new Random((int) (0x0000ffff & DateTime.UtcNow.Ticks));

        //    while (!token.IsCancellationRequested)
        //    {
        //        var ballot = ballotGenerator.New(nodeResolver.ResolveLocalNode());
        //        timer.Reset();
        //        timer.Start();
        //        var lease = leaseProvider.GetLease().Result;
        //        timer.Stop();
        //        if (lease != null)
        //        {
        //            logger.DebugFormat("[{4}] Requested Proposal: Timestamp {0}, process {1} === Received Lease: Leader {2} ExpiresAt {3} [{5}]",
        //                               ballot.Timestamp.ToString("HH:mm:ss fff"),
        //                               ballot.Process.Id,
        //                               lease.Owner.Id,
        //                               lease.ExpiresAt.ToString("HH:mm:ss fff"),
        //                               DateTime.UtcNow.ToString("HH:mm:ss fff"),
        //                               timer.ElapsedMilliseconds);
        //        }
        //        else
        //        {
        //            logger.DebugFormat("[{2}] Requested Proposal: Timestamp {0}, process {1} === Received Lease: NULL [{3}]",
        //                               ballot.Timestamp.ToString("HH:mm:ss fff"),
        //                               ballot.Process.Id,
        //                               DateTime.UtcNow.ToString("HH:mm:ss fff"),
        //                               timer.ElapsedMilliseconds);
        //        }
        //        //if (lease != null)
        //        //{
        //            Thread.Sleep(TimeSpan.FromMilliseconds(rnd.Next(20, 100)));
        //        //}
        //    }
        //}

        public void Start()
        {
            //Task.Factory.StartNew(() => ApplyCommands(token.Token), token.Token);
        }

        public void Stop()
        {
            nodeResolver.Dispose();
            replicationDemultiplexor.Dispose();
        }

        public int Id
        {
            get { return nodeResolver.ResolveLocalNode().Id; }
        }
    }
}