using System;
using System.Diagnostics;
using System.Reactive.Linq;
using wacs.Communication.Hubs.Intercom;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;
using Process = wacs.Configuration.Process;

namespace wacs.Rsm.Implementation
{
    public partial class Acceptor : IAcceptor
    {
        private readonly ILogger logger;
        private readonly IListener listener;
        private readonly IReplicatedLog replicatedLog;
        private Ballot minProposal;
        private Ballot acceptedProposal;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly ILeaseProvider leaseProvider;
        private readonly INodeResolver nodeResolver;
        private readonly object locker = new object();

        public Acceptor(IIntercomMessageHub intercomMessageHub,
                        IReplicatedLog replicatedLog,
                        ILeaseProvider leaseProvider,
                        INodeResolver nodeResolver,
                        ILogger logger)
        {
            this.logger = logger;
            this.replicatedLog = replicatedLog;
            this.intercomMessageHub = intercomMessageHub;
            this.nodeResolver = nodeResolver;
            this.leaseProvider = leaseProvider;
            minProposal = new Ballot(0);

            listener = intercomMessageHub.Subscribe();

            listener.Where(m => m.Body.MessageType == RsmPrepare.MessageType)
                    .Subscribe(new MessageStreamListener(OnPrepareReceived));
            listener.Where(m => m.Body.MessageType == RsmAccept.MessageType)
                    .Subscribe(new MessageStreamListener(OnAcceptReceived));

            listener.Start();
        }

        private void OnAcceptReceived(IMessage message)
        {
            try
            {

                var payload = new RsmAccept(message).GetPayload();
                var proposal = new Ballot(payload.Proposal.ProposalNumber);

                IMessage response;

                var timer = new Stopwatch();
                timer.Start();


                if (RequestCameNotFromLeader(new Process(message.Envelope.Sender.Id)))
                {
                    response = CreateNackAcceptNotLeaderMessage(payload);
                }
                else
                {
                    lock (locker)
                    {
                        response = RespondOnAcceptRequest(payload, proposal);
                    }
                }

                timer.Stop();
                logger.InfoFormat("Accept response in {0} msec", timer.ElapsedMilliseconds);

                intercomMessageHub.Send(new Process(message.Envelope.Sender.Id), response);
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private void OnPrepareReceived(IMessage message)
        {
            try
            {

                var payload = new RsmPrepare(message).GetPayload();
                var proposal = new Ballot(payload.Proposal.ProposalNumber);

                IMessage response;

                var timer = new Stopwatch();
                timer.Start();


                if (RequestCameNotFromLeader(new Process(message.Envelope.Sender.Id)))
                {
                    response = CreateNackPrepareNotLeaderMessage(payload);
                }
                else
                {
                    lock (locker)
                    {
                        response = RespondOnPrepareRequest(payload, proposal);
                    }
                }


                timer.Stop();
                logger.InfoFormat("Prepare response in {0} msec", timer.ElapsedMilliseconds);


                intercomMessageHub.Send(new Process(message.Envelope.Sender.Id), response);
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        public void Dispose()
        {
            listener.Stop();
            listener.Dispose();
        }
    }
}