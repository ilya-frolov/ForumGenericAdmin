using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;

namespace Dino.Common.Helpers
{
    public static class ColorHelpers
    {
        public static Color GetColorFromString(string input)
        {
            // Hash the string using MD5
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Use the first three bytes of the hash to create an RGB color
                int red = hashBytes[0];
                int green = hashBytes[1];
                int blue = hashBytes[2];

                return Color.FromArgb(red, green, blue);
            }
        }

        public static List<Color> GenerateDistinctColors(int count)
        {
            var random = new Random();
            var colors = new List<Color>();

            for (int i = 0; i < count; i++)
            {
                // Generate distinct colors by spreading values across the RGB spectrum
                int red = random.Next(0, 256);
                int green = random.Next(0, 256);
                int blue = random.Next(0, 256);

                // Make sure colors are distinct by forcing a gap between them
                red = (red + (i * 60)) % 256;
                green = (green + (i * 60)) % 256;
                blue = (blue + (i * 60)) % 256;

                colors.Add(Color.FromArgb(red, green, blue));
            }

            return colors;
        }
    }
}
