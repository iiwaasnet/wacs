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
			farm = CreateFarm(10).ToArray();
		}

		private void Join(IEnumerable<PaxosMachine> paxosMachines)
		{
			foreach (var paxosMachine in paxosMachines)
			{
				paxosMachine.JoinGroup(paxosMachines.ToArray());
			}
		}

		private IEnumerable<PaxosMachine> CreateFarm(int count)
		{
			for (var i = 0; i < count; i++)
			{
				//Thread.Sleep(TimeSpan.FromMilliseconds(100));
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
			Thread.Sleep(TimeSpan.FromSeconds(5));
			Task.Factory.StartNew(ElectLeader);
		}

		private void ElectLeader()
		{
			Join(farm);
			Console.WriteLine("Farm ========================================= ");

			var results = new List<WaitHandle>();
			foreach (var paxosMachine in farm)
			{
				results.Add(paxosMachine.ElectLeader(TimeSpan.FromSeconds(4)));

				Console.WriteLine("[{0}] Node {1}, Age {2}, LastAppliedLogEntry {3}",
				                  DateTime.Now.ToString("hh:mm:ss fff"),
				                  paxosMachine.Id,
				                  paxosMachine.Age,
				                  paxosMachine.LastAppliedLogEntry);
			}

			Console.WriteLine("Election results ========================================= ");

			foreach (var waitHandle in results)
			{
				waitHandle.WaitOne();
			}

			foreach (var paxosMachine in farm)
			{
				var result = paxosMachine.GetElectionResult();
				if (result.Status == CampaignStatus.Elected)
				{
					Console.WriteLine("[{0}] Leader Id {1}, Age {2}, LastAppliedLogEntry {3}",
									  DateTime.Now.ToString("hh:mm:ss fff"),
					                  result.Leader.Id,
					                  result.Leader.Age,
					                  result.Leader.LastAppliedLogEntry);
				}
				else
				{
					Console.WriteLine("Status {0}", result.Status);
				}
			}
			Console.WriteLine("====== Propose messages - {0}, Accept messages - {1} =======", ProposeMessage.Count, AcceptMessage.Count);
		}

		public void Stop()
		{
			foreach (var paxosMachine in farm)
			{
				paxosMachine.Stop();
			}
		}
	}
}