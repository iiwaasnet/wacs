using System.Collections;
using System.Collections.Generic;

namespace wacs.ResolutionService.Interface
{
    public interface IResolutionService
    {
        IEnumerable<IProcess> GetWorld();

        IProcess GetLocalProcess();
    }
}