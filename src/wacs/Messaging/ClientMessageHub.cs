using wacs.Rsm.Interface;

namespace wacs.Messaging
{
    public class ClientMessageHub : IClientMessageHub
    {
        private readonly IClientMessageRouter messageRouter;
        private readonly ISynodConfigurationProvider synodConfigProvider;

        public ClientMessageHub(ISynodConfigurationProvider synodConfigProvider, IClientMessageRouter messageRouter)
        {
            this.synodConfigProvider = synodConfigProvider;
            this.messageRouter = messageRouter;
        }
    }
}