namespace wacs.Election
{
	public class Candidate
	{
		public long Age { get; set; }
		public long LastAppliedLogEntry { get; set; }
		public string Id { get; set; }

		protected bool Equals(Candidate other)
		{
			return Age == other.Age
			       && LastAppliedLogEntry == other.LastAppliedLogEntry
			       && Id == other.Id;
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
			return Equals((Candidate) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				long hashCode = Age.GetHashCode();
				hashCode = (hashCode * 397) ^ LastAppliedLogEntry;
				hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
				return (int) hashCode & 0x0000ffff;
			}
		}

		public bool BetterThan(Candidate other)
		{
			return (other == null)
			       || LastAppliedLogEntry > other.LastAppliedLogEntry
			       || (LastAppliedLogEntry == other.LastAppliedLogEntry && Age > other.Age);
		}

		public bool WorseThan(Candidate other)
		{
			return other != null
			       && (other.LastAppliedLogEntry > LastAppliedLogEntry
			           || (other.LastAppliedLogEntry == LastAppliedLogEntry && other.Age > Age));
		}

		public bool SameGood(Candidate other)
		{
			return other != null
			       && LastAppliedLogEntry == other.LastAppliedLogEntry
			       && Age == other.Age;
		}
	}
}