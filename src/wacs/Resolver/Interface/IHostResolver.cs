using System.Collections.Generic;

namespace wacs.Resolver.Interface
{
    public interface IHostResolver
    {
        //IEnumerable<INode> GetWorld();

        INode ResolveLocalProcess();

        INode ResolveRemoteProcess(Configuration.INode node);

        void Start();

        void Stop();
    }
}