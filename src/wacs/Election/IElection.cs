using System;
using System.Threading.Tasks;

namespace wacs.Election
{
	public interface IElection
	{
		Task<ElectionResult> Elect(TimeSpan timeout);
	}
}