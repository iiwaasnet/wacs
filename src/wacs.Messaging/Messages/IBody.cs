namespace wacs.Messaging.Messages
{
	public interface IBody
	{
		string MessageType { get; }

		byte[] Content { get; }
	}
}