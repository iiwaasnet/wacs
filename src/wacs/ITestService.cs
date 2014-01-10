namespace wacs
{
    public interface ITestService
    {
        void Start();

        void Stop();

        int Id { get; }
    }
}