using System;

namespace NFMWorld.ShaderSourceGen;

internal static class Helpers
{
    extension(string)
    {
        public static unsafe string FromSpan(ReadOnlySpan<char> span)
        {
            fixed (char* ptr = span)
            {
                return new string(ptr, 0, span.Length);
            }

            return null;
        }
    }

    extension(int)
    {
        public static int Parse(ReadOnlySpan<char> s, IFormatProvider provider)
        {
            return int.Parse(string.FromSpan(s), provider);
        }
    }
}