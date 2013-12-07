using System.Threading;

namespace wacs.core.State
{
    public interface IObservableCondition
    {
        WaitHandle Waitable { get; } 
    }
}