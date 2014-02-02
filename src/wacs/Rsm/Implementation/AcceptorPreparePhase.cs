using System.Diagnostics;
using wacs.Configuration;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public partial class Acceptor
    {
        private IMessage RespondOnPrepareRequest(RsmPrepare.Payload payload, Ballot proposal)
        {
            IMessage response;

            var logEntry = replicatedLog.GetLogEntry(new LogIndex(payload.LogIndex.Index));

            if (logEntry.State == LogEntryState.Chosen)
            {
                response = CreateNackPrepareAlreadyChosenMessage(payload);
            }
            else
            {
                var timer = new Stopwatch();
                timer.Start();

                response = RespondOnUnchosenLogEntry(payload, proposal, logEntry);

                timer.Stop();
                logger.InfoFormat("RespondOnPrepareRequest in {0} msec", timer.ElapsedMilliseconds);
            }
            return response;
        }

        private IMessage RespondOnUnchosenLogEntry(RsmPrepare.Payload payload, Ballot proposal, ILogEntry logEntry)
        {
            IMessage response;

            if (proposal > minProposal)
            {
                minProposal = proposal;
                response = CreateAckPrepareMessage(payload, logEntry);
            }
            else
            {
                response = CreateNackPrepareMessage(payload);
            }

            return response;
        }

        private bool RequestCameNotFromLeader(IProcess sender)
        {
            return !sender.Equals(leaseProvider.GetLease().Owner);
        }
    }
}