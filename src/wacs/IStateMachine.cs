namespace wacs
{
	public interface IStateMachine
	{
		void Start();

		void Stop();

		int Id { get; }
	}
}