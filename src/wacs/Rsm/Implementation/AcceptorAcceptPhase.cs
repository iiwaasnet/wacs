using System.Diagnostics;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public partial class Acceptor
    {
        private IMessage RespondOnAcceptRequest(RsmAccept.Payload payload, Ballot proposal)
        {
            IMessage response;

            var logEntry = replicatedLog.GetLogEntry(new LogIndex(payload.LogIndex.Index));

            if (logEntry.State == LogEntryState.Chosen)
            {
                response = CreateNackAcceptAlreadyChosenMessage(payload);
            }
            else
            {
                var timer = new Stopwatch();
                timer.Start();

                response = RespondOnUnchosenLogEntry(payload, proposal);

                timer.Stop();
                logger.InfoFormat("RespondOnAcceptRequest in {0} msec", timer.ElapsedMilliseconds);
            }
            return response;
        }

        private IMessage RespondOnUnchosenLogEntry(RsmAccept.Payload payload, Ballot proposal)
        {
            IMessage response;

            if (proposal >= minProposal)
            {
                minProposal = acceptedProposal = proposal;
                replicatedLog.SetLogEntryAccepted(new LogIndex(payload.LogIndex.Index),
                                                  new Message(payload.Value.Envelope, payload.Value.Body));
                response = CreateAckAcceptMessage(payload);
            }
            else
            {
                response = CreateNackAcceptMessage(payload);
            }

            return response;
        }
    }
}