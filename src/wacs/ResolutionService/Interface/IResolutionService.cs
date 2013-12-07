using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wacs.ResolutionService.Interface
{
    public interface IResolutionService
    {
        Task<IEnumerable<IProcess>> GetWorld();

        Task<IProcess> GetLocalProcess();
    }
}