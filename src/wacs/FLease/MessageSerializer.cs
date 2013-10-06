using System.Diagnostics.Contracts;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace wacs.FLease
{
	public class MessageSerializer : IMessageSerializer
	{
		public byte[] Serialize(object obj)
		{
			Contract.Requires(obj != null);

			using (var stream = new MemoryStream())
			{
				using (var writer = new BsonWriter(stream))
				{
					new JsonSerializer().Serialize(writer, obj);

					writer.Flush();

					return stream.GetBuffer();
				}
			}
		}

		public T Deserialize<T>(byte[] buffer)
		{
			Contract.Requires(buffer != null);

			using (var stream = new MemoryStream(buffer))
			{
				using (var reader = new BsonReader(stream))
				{
					return new JsonSerializer().Deserialize<T>(reader);
				}
			}
		}
	}
}