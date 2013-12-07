namespace wacs.core.State
{
    public interface IObservableAtomicValue<T> : IChangeNotifiable
    {
        void Set(T value);

        T Get();
    }
}