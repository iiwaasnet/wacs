namespace wacs.Messaging
{
	public interface IBody
	{
		string MessageType { get; }

		byte[] Content { get; }
	}
}