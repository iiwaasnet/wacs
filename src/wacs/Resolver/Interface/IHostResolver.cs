using System.Collections.Generic;

namespace wacs.Resolver.Interface
{
    public interface IHostResolver
    {
        IEnumerable<INode> GetWorld();

        INode GetLocalProcess();

        void Start();

        void Stop();
    }
}