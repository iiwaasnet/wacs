using wacs.Configuration;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ConsensusRoundManager : IConsensusRoundManager
    {
        private IBallot currentRoundBallot;
        public ConsensusRoundManager(ISynodConfigurationProvider configurationProvider)
        {
            currentRoundBallot = new Ballot(0, configurationProvider.LocalProcess);
        }

        public IBallot GetNextBallot()
        {
            return currentRoundBallot = currentRoundBallot.NewByIncrementing();
        }
    }
}