using System.Collections.Generic;
using System.Net;
using System.ServiceModel;

namespace wacs.Messaging
{
    public interface IMessageHubConfiguration
    {
        IEnumerable<EndpointAddress> Listeners { get; }
        EndpointAddress Sender { get; }
    }
}