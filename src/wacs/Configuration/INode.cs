namespace wacs.Configuration
{
    public interface INode
    {
        string GetServiceAddress();

        string GetIntercomAddress();

        string BaseAddress { get; }
        int IntercomPort { get; }
        int ServicePort { get; }
    }
}