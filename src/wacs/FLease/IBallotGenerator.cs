namespace wacs.FLease
{
	public interface IBallotGenerator
	{
		IBallot New(INode owner);

		IBallot Null();
	}
}