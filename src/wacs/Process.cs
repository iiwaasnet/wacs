namespace wacs
{
	public class Process : IProcess
	{
		public Process(int id)
		{
			Id = id;
		}

		public int Id { get; private set; }
	}
}