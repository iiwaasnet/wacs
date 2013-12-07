namespace wacs.core.State
{
    public delegate void ChangedEventHandler();

    public interface IChangeNotifiable
    {
        event ChangedEventHandler Changed;
    }
}