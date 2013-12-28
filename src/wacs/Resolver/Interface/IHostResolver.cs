using System.Collections.Generic;

namespace wacs.Resolver.Interface
{
    public interface IHostResolver
    {
        //IEnumerable<INode> GetWorld();

        INode ResolveLocalProcess();

        INode ResolveRemoteProcess(Configuration.IEndpoint endpoint);

        void Start();

        void Stop();
    }
}