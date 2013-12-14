using System.Collections.Generic;
using System.Threading.Tasks;

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