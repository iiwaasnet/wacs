namespace wacs.FLease
{
	public interface IBallotGenerator
	{
		IBallot New(IProcess owner);

		IBallot Null();
	}
}