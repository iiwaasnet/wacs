using System.Collections.Generic;
using System.Net;
using System.ServiceModel;

namespace wacs.Messaging
{
    public class MessageHubConfiguration : IMessageHubConfiguration
    {
        public IEnumerable<EndpointAddress> Listeners { get; set; }
        public EndpointAddress Sender { get; set; }
    }
}