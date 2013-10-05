namespace wacs.FLease
{
	public interface IMessageSerializer
	{
		byte[] Serialize(object obj);

		T Deserialize<T>(byte[] obj);
	}
}