using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BackBank.Internal
{
    public class BinaryUtils
    {
        public static string ToHexString(byte[] buffer)
        {
            return ToHexString(buffer.AsSpan());
        }

        public static string ToHexString(ReadOnlySpan<byte> span)
        {
            var builder = new StringBuilder();

            byte b;
            for (int i = 0; i < span.Length; ++i)
            {
                b = (byte)(span[i] >> 4);
                builder.Append((char)(b > 9 ? b + 0x37 : b + 0x30));
                b = (byte)(span[i] & 0x0F);
                builder.Append((char)(b > 9 ? b + 0x37 : b + 0x30));
            }

            return builder.ToString();
        }

        public static byte[] FromHexString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var len = value.Length;

            if ((len & 1) == 1)
            {
                throw new ArgumentException("Hex string should have even number of digits", nameof(value));
            }

            var bytes = new byte[len >> 1];

            for (int i = 0, j = 0; i < len; i += 2, ++j)
            {
                var firstHalf = ToHalfByte(value[i]);
                var secondHalf = ToHalfByte(value[i + 1]);

                bytes[j] = (byte)((firstHalf << 4) + secondHalf);
            }

            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToHalfByte(char c)
        {
            var integer = (int)c;

            if (c >= '0' && c <= '9')
                return integer - 48;

            if (c >= 'A' && c <= 'F')
                return integer - 55;

            if (c >= 'a' && c <= 'f')
                return integer - 87;

            throw new ArgumentException($"Invalid hex digit '{c}'");
        }
    }
}
