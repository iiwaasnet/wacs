using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using wacs.Election;

namespace wacs
{
	public class WACService : IService
	{
		private readonly IEnumerable<PaxosMachine> farm;

		public WACService()
		{
			farm = CreateFarm(5).ToArray();
			Join(farm);
		}

		private void Join(IEnumerable<PaxosMachine> paxosMachines)
		{
			foreach (var paxosMachine in paxosMachines)
			{
				paxosMachine.JoinGroup(paxosMachines.Where(m => m != paxosMachine).ToArray());
			}
		}

		private IEnumerable<PaxosMachine> CreateFarm(int count)
		{
			for (var i = 0; i < count; i++)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
				yield return new PaxosMachine(i.ToString(), GenerateLastAppliedLogEntry());
			}
		}

		private int GenerateLastAppliedLogEntry()
		{
			var rnd = new Random((int) DateTime.UtcNow.Ticks & 0x0000ffff);

			return rnd.Next(1, 5);
		}

		public void Start()
		{
			Task.Factory.StartNew(ElectLeader);
		}

		private void ElectLeader()
		{
			Console.WriteLine("Farm ========================================= ");

			var results = new List<Task<ElectionResult>>();
			foreach (var paxosMachine in farm)
			{
				results.Add(paxosMachine.ElectLeader(TimeSpan.FromSeconds(1)));

				Console.WriteLine("Node {0}, Age {1}, LastAppliedLogEntry {2}",
					paxosMachine.Id, paxosMachine.Age, paxosMachine.LastAppliedLogEntry);
			}

			Console.WriteLine("Election results ========================================= ");

			foreach (var result in results)
			{
				if (result.Result.Status == CampaignStatus.Elected)
				{
					Console.WriteLine("Leader Id {0}, Age {1}, LastAppliedLogEntry {2}",
					                  result.Result.Leader.Id,
					                  result.Result.Leader.Age,
					                  result.Result.Leader.LastAppliedLogEntry);
				}
				else
				{
					Console.WriteLine("Status {0}", result.Result.Status);
				}
			}
		}

		public void Stop()
		{
		}
	}
}