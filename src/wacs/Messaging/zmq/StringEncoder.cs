using System.Text;

namespace wacs.Messaging.zmq
{
    public static class StringEncoder
    {
        private static readonly Encoding encoder;

        static StringEncoder()
        {
            encoder = Encoding.UTF8;
        }

        public static string GetString(this byte[] array)
        {
            return encoder.GetString(array);
        }

        public static byte[] GetBytes(this string str)
        {
            return encoder.GetBytes(str);
        }
    }
}