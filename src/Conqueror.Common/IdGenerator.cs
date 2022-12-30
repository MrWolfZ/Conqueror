using System;
using System.Threading;

namespace Conqueror.Common
{
    // copied from here: https://github.com/dotnet/aspnetcore/blob/0467d031e798d596420dcf54a056bc6ab690e437/src/Servers/Kestrel/shared/CorrelationIdGenerator.cs
    internal static class IdGenerator
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly char[] Encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();

        // Seed the lastId for this application instance with the number of 100-nanosecond intervals that
        // have elapsed since 12:00:00 midnight, January 1, 0001 for a roughly increasing lastId over restarts
        private static long lastId = DateTime.UtcNow.Ticks;

        public static string GetNextId() => GenerateId(Interlocked.Increment(ref lastId));

        private static string GenerateId(long id)
        {
            return string.Create(13, id, (buffer, value) =>
            {
                buffer[12] = Encode32Chars[value & 31];
                buffer[11] = Encode32Chars[(value >> 5) & 31];
                buffer[10] = Encode32Chars[(value >> 10) & 31];
                buffer[9] = Encode32Chars[(value >> 15) & 31];
                buffer[8] = Encode32Chars[(value >> 20) & 31];
                buffer[7] = Encode32Chars[(value >> 25) & 31];
                buffer[6] = Encode32Chars[(value >> 30) & 31];
                buffer[5] = Encode32Chars[(value >> 35) & 31];
                buffer[4] = Encode32Chars[(value >> 40) & 31];
                buffer[3] = Encode32Chars[(value >> 45) & 31];
                buffer[2] = Encode32Chars[(value >> 50) & 31];
                buffer[1] = Encode32Chars[(value >> 55) & 31];
                buffer[0] = Encode32Chars[(value >> 60) & 31];
            });
        }
    }
}
