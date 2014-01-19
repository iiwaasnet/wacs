using System;
using System.Collections.Generic;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class WacsStateMachine : IReplicatedStateMachine
    {
        private readonly IDictionary<string, Action<ISyncCommand>> messageHandlers;

        public WacsStateMachine()
        {
            messageHandlers = CreateMessageHandlersMap();
        }

        public void ProcessCommand(ISyncCommand command)
        {
            Action<ISyncCommand> handler;

            if (messageHandlers.TryGetValue(command.Request.Body.MessageType, out handler))
            {
                handler(command);
            }
        }

        private void CreateNode(ISyncCommand command)
        {
        }

        private IDictionary<string, Action<ISyncCommand>> CreateMessageHandlersMap()
        {
            return new Dictionary<string, Action<ISyncCommand>>
                   {
                       {Messaging.Messages.Client.wacs.CreateNode.MessageType, CreateNode}
                   };
        }
    }
}