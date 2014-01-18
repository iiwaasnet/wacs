using System;

namespace wacs.Framework.State
{
    public interface IAwaitableResponse<out T> : IDisposable
    {
        T GetResponse();

        T GetResponse(TimeSpan timeout);
    }
}