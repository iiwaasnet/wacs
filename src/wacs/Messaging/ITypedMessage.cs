﻿namespace wacs.Messaging
{
    public interface ITypedMessage<out T>: IMessage
    {
        T GetPayload();
    }
}