﻿namespace wacs.Messaging
{
	public interface IMessageHub
	{
		IListener Subscribe(IMessageSink messageSink);

		void Broadcast(IMessage message);

		void Send(IProcess process, IMessage message);
	}
}