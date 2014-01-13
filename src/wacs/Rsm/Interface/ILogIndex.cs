using System;

namespace wacs.Rsm.Interface
{
    public interface ILogIndex : IComparable
    {
        ILogIndex Increment();
        ulong Index { get; }
    }
}