namespace wacs
{
    public class Process : IProcess
    {
        public Process(string id)
        {
            Id = id;
        }

        public static bool operator ==(Process x, Process y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Process x, Process y)
        {
            return !(x == y);
        }

        protected bool Equals(Process other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Process) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public string Id { get; private set; }
    }
}