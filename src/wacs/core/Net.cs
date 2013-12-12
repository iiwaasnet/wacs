using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using wacs.Configuration;

namespace wacs.core
{
    public static class Net
    {
        public static string GetLocalEndpoint(this IEnumerable<Configuration.INode> nodes)
        {
            var endpoint = GetLocalConfiguredEndpoint(nodes);

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = GetLocalResolvedEndpoint(nodes);
            }

            return endpoint.TrimEnd('/');
        }

        private static string GetLocalResolvedEndpoint(IEnumerable<Configuration.INode> nodes)
        {
            var localIP = Dns.GetHostEntry(Dns.GetHostName())
                             .AddressList
                             .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                                   || ip.AddressFamily == AddressFamily.InterNetworkV6);

            if (localIP == null)
            {
                throw new Exception("Unable to resolve host external IP address!");
            }

            var uri = nodes
                .Select(n => new Uri(n.Address, UriKind.Absolute))
                .FirstOrDefault(n => n.Host == localIP.ToString());

            if (uri == null)
            {
                throw new Exception("Host is not configured to be part of the cluster!");
            }

            return uri.AbsoluteUri;
        }

        private static string GetLocalConfiguredEndpoint(IEnumerable<Configuration.INode> nodes)
        {
            var uri = nodes
                .Where(n => n.IsLocal)
                .Select(n => new Uri(n.Address).AbsoluteUri)
                .FirstOrDefault();

            return uri;
        }

    }
}