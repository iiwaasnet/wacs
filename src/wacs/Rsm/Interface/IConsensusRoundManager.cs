namespace wacs.Rsm.Interface
{
    public interface IConsensusRoundManager
    {
        IBallot GetNextBallot();
        void SetMinBallot(IBallot minProposal);
    }
}