using System;

namespace wacs.core
{
    public class UniqueIdGenerator
    {
        public static int Generate(int length)
        {
            var rnd = new Random((int)DateTime.UtcNow.Ticks & 0x0000ffff);
            var id = 0;

            for (var i = 0; i < length; i++)
            {
                id += rnd.Next(0, 9) * (int)Math.Pow(10, i);
            }

            return id;
        }

        public static string Generate()
        {
            return Base62ToString(DateTime.UtcNow.Ticks);
        }

        private static string Base62ToString(long value)
        {
            // Divides the number by 64, so how many 64s are in
            // 'value'. This number is stored in Y.
            // e.g #1
            // 1) 1000 / 62 = 16, plus 8 remainder (stored in x).
            // 2) 16 / 62 = 0, remainder 16
            // 3) 16, 8 or G8:
            // 4) 65 is A, add 6 to this = 71 or G.
            //
            // e.g #2:
            // 1) 10000 / 62 = 161, remainder 18
            // 2) 161 / 62 = 2, remainder 37
            // 3) 2 / 62 = 0, remainder 2
            // 4) 2, 37, 18, or 2,b,I:
            // 5) 65 is A, add 27 to this (minus 10 from 37 as these are digits) = 92.
            //    Add 6 to 92, as 91-96 are symbols. 98 is b.
            // 6)
            var x = 0L;
            var y = Math.DivRem(value, 62, out x);

            if (y > 0)
            {
                return Base62ToString(y) + ValToChar(x).ToString();
            }

            return ValToChar(x).ToString();
        }

        private static char ValToChar(long value)
        {
            if (value > 9)
            {
                var ascii = (65 + ((int) value - 10));
                if (ascii > 90)
                {
                    ascii += 6;
                }

                return (char) ascii;
            }

            return value.ToString()[0];
        }
    }
}