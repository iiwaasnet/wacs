using System;

namespace wacs.Rsm.Interface
{
    public interface ILogIndex : IComparable
    {
        ILogIndex Increment();
        ILogIndex Dicrement();
        ulong Index { get; }
    }
}