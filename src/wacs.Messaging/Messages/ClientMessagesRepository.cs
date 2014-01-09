using System;
using System.Collections.Generic;

namespace wacs.Messaging.Messages
{
    public class ClientMessagesRepository : IClientMessagesRepository
    {
        private readonly IDictionary<string, object> quorumAcceptableMessages;

        public ClientMessagesRepository()
        {
            quorumAcceptableMessages = CreateListOfQuorumAcceptableMessages();
        }

        private IDictionary<string, object> CreateListOfQuorumAcceptableMessages()
        {
            return new Dictionary<string, object>();
        }

        public bool RequiresQuorum(IMessage message)
        {
            return quorumAcceptableMessages.ContainsKey(message.Body.MessageType);
        }
    }
}