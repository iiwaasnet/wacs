using System.Threading;

namespace wacs.Framework.State
{
    public interface IObservableCondition
    {
        WaitHandle Waitable { get; } 
    }
}