namespace wacs
{
	public interface IStateMachine
	{
		void Start();

		void Stop();

		string Id { get; }
	}
}