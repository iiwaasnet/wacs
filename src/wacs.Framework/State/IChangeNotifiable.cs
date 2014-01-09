namespace wacs.Framework.State
{
    public delegate void ChangedEventHandler();

    public interface IChangeNotifiable
    {
        event ChangedEventHandler Changed;
    }
}