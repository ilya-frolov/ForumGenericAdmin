using System;
using System.Security.Cryptography;

namespace Dino.Common.Security
{
    public static class SecureNumberGenerator
    {
        public static int GetInt(int min, int max)
        {
            // Generate four random bytes
            byte[] four_bytes = new byte[4];
            RandomNumberGenerator.Create().GetBytes(four_bytes);

            // Convert the bytes to a UInt32
            UInt32 scale = BitConverter.ToUInt32(four_bytes, 0);

            // And use that to pick a random number >= min and < max
            return (int)(min + (max - min) * (scale / (uint.MaxValue + 1.0)));
        }
    }
}
