namespace wacs
{
    public interface IBootstrapper
    {
        void Start();

        void Stop();

        int Id { get; }
    }
}