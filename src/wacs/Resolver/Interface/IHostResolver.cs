using System.Collections.Generic;
using System.Threading.Tasks;

namespace wacs.Resolver.Interface
{
    public interface IHostResolver
    {
        Task<IEnumerable<IProcess>> GetWorld();

        Task<IProcess> GetLocalProcess();

        void Start();

        void Stop();
    }
}