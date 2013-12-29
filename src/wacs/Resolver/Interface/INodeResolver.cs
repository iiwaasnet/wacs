using wacs.Configuration;

namespace wacs.Resolver.Interface
{
    public interface INodeResolver
    {
        IProcess ResolveLocalNode();

        IProcess ResolveRemoteNode(INode node);
        INode ResolveRemoteProcess(IProcess process);

        void Start();

        void Stop();
    }
}