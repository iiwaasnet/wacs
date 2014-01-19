namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class LogIndex
    {
        //protected bool Equals(LogIndex other)
        //{
        //    return Index == other.Index;
        //}

        //public override bool Equals(object obj)
        //{
        //    if (ReferenceEquals(null, obj))
        //    {
        //        return false;
        //    }
        //    if (ReferenceEquals(this, obj))
        //    {
        //        return true;
        //    }
        //    if (obj.GetType() != this.GetType())
        //    {
        //        return false;
        //    }
        //    return Equals((LogIndex) obj);
        //}

        //public override int GetHashCode()
        //{
        //    return Index.GetHashCode();
        //}

        public ulong Index { get; set; }
    }
}