namespace wacs.Election
{
	public interface IElector
	{
		void Propose(Candidate candidate);
		void Accepted(Candidate candidate, Candidate elector);
		//event ElectionEvent Proposed;
		//event ElectionEvent Accept;
	}
}