using System.Collections.Generic;

namespace wacs
{
	public class WACService : IService
	{
		private readonly IEnumerable<IStateMachine> farm;

		public WACService(IEnumerable<IStateMachine> members)
		{
			farm = members;
		}

		public void Start()
		{
			foreach (var stateMachine in farm)
			{
				stateMachine.Start();
			}
		}

		public void Stop()
		{
			foreach (var stateMachine in farm)
			{
				stateMachine.Stop();
			}
		}
	}
}