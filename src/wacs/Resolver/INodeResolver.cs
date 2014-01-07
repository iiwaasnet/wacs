using System;
using wacs.Configuration;

namespace wacs.Resolver
{
    public interface INodeResolver : IDisposable
    {
        IProcess ResolveLocalNode();
        IProcess ResolveRemoteNode(INode node);
        INode ResolveRemoteProcess(IProcess process);
    }
}