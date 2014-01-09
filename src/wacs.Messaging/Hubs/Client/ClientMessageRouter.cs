using System;
using Castle.Core.Logging;
using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Client
{
    public class ClientMessageRouter : IClientMessageRouter
    {
        private readonly IClientMessageHubConfiguration config;
        private readonly ILogger logger;

        public ClientMessageRouter( ISynodConfigurationProvider configurationProvider, IClientMessageHubConfiguration config, ILogger logger)
        {
            this.logger = logger;
            this.config = config;

        }

        public IMessage ForwardClientRequestToLeader(IMessage message)
        {
            throw new NotImplementedException();
        }
    }
}