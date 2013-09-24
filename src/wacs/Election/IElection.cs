using System;
using System.Threading;

namespace wacs.Election
{
	public interface IElection
	{
		WaitHandle Elect(TimeSpan timeout);
	}
}