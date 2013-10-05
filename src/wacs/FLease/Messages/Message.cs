using wacs.Messaging;

namespace wacs.FLease.Messages
{
	public class Message : IMessage
	{
		public IEnvelope Envelope { get; set; }
		public IBody Body { get; set; }
	}
}