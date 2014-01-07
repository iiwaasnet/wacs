using wacs.Configuration;
using wacs.Messaging;
using wacs.Messaging.Messages;

namespace wacs.FLease
{
	public interface IBallotGenerator
	{
		IBallot New(IProcess owner);

		IBallot Null();
	}
}