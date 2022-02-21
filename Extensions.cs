using System;
using System.Collections.Generic;

namespace CppEmbeddedHeaderGenerator
{
    public static class Extensions
    {
        /**
         *  Split a string into equal sized chunks.
         *  The last chunk will contain whatever remains.
         *  
         *  Source: https://stackoverflow.com/a/1450889
         */
        public static IEnumerable<string> SplitChunks(this string str, int chunkSize)
        {
            for (int i = 0; i < str.Length; i += chunkSize)
                yield return str.Substring(i, Math.Min(chunkSize, str.Length - i));
        }
    }
}
