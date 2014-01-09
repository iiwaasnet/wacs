using System;
using System.Threading;

namespace wacs.Framework.State
{
    public interface IAwaitableResult<out T>: IDisposable
    {
        T GetResult();
        T GetResult(TimeSpan timeout);
    }
}