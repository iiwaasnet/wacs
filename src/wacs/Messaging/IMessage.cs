﻿namespace wacs.Messaging
{
	public interface IMessage
	{
		IEnvelope Envelope { get; }

		IBody Body { get; }
	}
}