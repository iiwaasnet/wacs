namespace wacs.Configuration
{
    public interface INode
    {
        string Address { get; }
        bool IsLocal { get; }
    }
}