using System;
using System.Collections.Generic;
using System.Threading;
using wacs.Messaging.Messages.Client.wacs;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class WacsStateMachine : IReplicatedStateMachine
    {
        private readonly IDictionary<string, Action<ISyncCommand>> messageHandlers;
        private int nextNodeIndex;
        private readonly INodeResolver nodeResolver;

        public WacsStateMachine(INodeResolver nodeResolver)
        {
            messageHandlers = CreateMessageHandlersMap();
            nextNodeIndex = 0;
            this.nodeResolver = nodeResolver;
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
            var response = new CreateNodeResponse(nodeResolver.ResolveLocalNode(),
                                                  new CreateNodeResponse.Payload
                                                  {
                                                      NodeIndex = Interlocked.Increment(ref nextNodeIndex)
                                                  });
            ((AwaitableRsmRequest)command).SetResponse(response);
        }

        private IDictionary<string, Action<ISyncCommand>> CreateMessageHandlersMap()
        {
            return new Dictionary<string, Action<ISyncCommand>>
                   {
                       {CreateNodeRequest.MessageType, CreateNode}
                   };
        }
    }
}