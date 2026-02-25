using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Dino.Mvc.Common.Helpers
{
	public static class HttpPostedFileHelpers
	{
		/// <summary>
		/// Converts an input stream from an HttpPostedFileBase object, to MemoryStream object.
		/// </summary>
		/// <param name="httpPostedFileBase">The HttpPostedFileBase object.</param>
		/// <returns>The file as MemoryStream object.</returns>
		public static MemoryStream GetFileAsMemoryStream(this IFormFile httpPostedFileBase)
		{
			MemoryStream memoryStream = null;
			using (Stream inputStream = httpPostedFileBase.OpenReadStream())
			{
				// Check if the input-stream as already memory-stream.
				memoryStream = inputStream as MemoryStream;
				if (memoryStream == null)
				{
					// Get as memory-stream.
					memoryStream = new MemoryStream();
					inputStream.CopyTo(memoryStream);
				}
			}

			return memoryStream;
		}

		/// <summary>
		/// Converts an input stream from an HttpPostedFileBase object, to byte-array.
		/// </summary>
		/// <param name="httpPostedFileBase">The HttpPostedFileBase object.</param>
		/// <returns>The file as byte-array.</returns>
		public static byte[] GetFileAsByteArray(this IFormFile httpPostedFileBase)
		{
			byte[] data = null;
			var stream = httpPostedFileBase.GetFileAsMemoryStream();
			if (stream != null)
			{
				data = stream.ToArray();
			}
			else
			{
				// TODO: Better exception handling.
				throw new Exception("Stream is NULL when trying to convert to byte-array.");
			}

			return (data);
		}
	}
}