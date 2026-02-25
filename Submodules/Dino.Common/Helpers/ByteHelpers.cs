using System.Linq;

namespace Dino.Common.Helpers
{
	public static class ByteHelpers
	{
		/// <summary>
		/// Combines multiple byte arrays to a single array
		/// </summary>
		/// <param name="arrays">The byte arrays</param>
		/// <returns>Single combined byte array</returns>
		public static byte[] Combine(params byte[][] arrays)
		{
			var rv = new byte[arrays.Sum(a => a.Length)];
			var offset = 0;
			foreach (var array in arrays)
			{
				System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
				offset += array.Length;
			}
			return rv;
		}
	}
}
