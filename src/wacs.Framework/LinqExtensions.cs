using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace wacs.Framework
{
    public static class LinqExtensions
    {
        public static T Second<T>(this IEnumerable<T> collection)
        {
            return collection.Skip(1).First();
        }

        public static T Third<T>(this IEnumerable<T> collection)
        {
            return collection.Skip(2).First();
        }
    }
}