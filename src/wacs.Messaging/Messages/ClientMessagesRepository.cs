using System.Collections.Generic;
using wacs.Messaging.Messages.Client.wacs;

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
            return new Dictionary<string, object>
                   {
                       {CreateNodeRequest.MessageType, null}
                   };
        }

        public bool RequiresQuorum(IMessage message)
        {
            return quorumAcceptableMessages.ContainsKey(message.Body.MessageType);
        }
    }
}