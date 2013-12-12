using System.Collections.Generic;
using System.Threading.Tasks;

namespace wacs.Resolver.Interface
{
    public interface IHostResolver
    {
        Task<IEnumerable<INode>> GetWorld();

        Task<INode> GetLocalProcess();

        void Start();

        void Stop();
    }
}