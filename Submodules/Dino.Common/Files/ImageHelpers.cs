using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Dino.Common.Files
{
	public static class ImageHelpers
	{
		#region Resize

		/// <summary>
		/// Resizes an image.
		/// </summary>
		/// <param name="bitmap">The bitmap of the image to resize.</param>
		/// <param name="width">The wanted width, or -1 if we should scale according to the height.</param>
		/// <param name="height">The wanted height, or -1 if we should scale according to the width.</param>
		/// <param name="keepProportions">Do we need to keep the proportions of the image.
		/// Relevant only if width and height are set.</param>
		/// <param name="biggerIfNeeded">Do we need to resize to bigger proportion, if possible.</param>
		/// <returns>The resized bitmap.</returns>
		public static Bitmap Resize(this Bitmap bitmap, int width, int height, bool keepProportions,
									bool biggerIfNeeded = false)
		{
		    var newImage = bitmap;

			var finalWidth = width;
			var finalHeight = height;

			// Check if scaling is needed.
		    int widthComparison = bitmap.Width.CompareTo(width);
            int heightComparison = bitmap.Height.CompareTo(height);

			if (((widthComparison > 0) || (heightComparison > 0)) || 
                (((widthComparison != 0) || (heightComparison != 0)) && (biggerIfNeeded)))
			{
                // Check if need to save the proportions.
			    if (keepProportions)
			    {
			        double scale = 0.0;

			        // Scale by the smallest scale.
			        if ((((double) width/bitmap.Width) > ((double) height/bitmap.Height)) || 
                        ((height == -1) && (width != -1)))
			        {
			            finalWidth = width;
                        finalHeight = (int)Math.Ceiling(bitmap.Height * ((double)width / bitmap.Width));
			        }
			        else
			        {
                        finalWidth = (int)Math.Ceiling(bitmap.Width * ((double)height / bitmap.Height));
                        finalHeight = height;
			        }
			    }

                // Resize.
                newImage = new Bitmap(finalWidth, finalHeight);
                using (var gr = Graphics.FromImage(newImage))
                {
                    gr.SmoothingMode = SmoothingMode.HighQuality;
                    gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    gr.DrawImage(bitmap, new Rectangle(0, 0, finalWidth, finalHeight));
                }
			}

			// Return the resized image.
			return (newImage);
		}

		#endregion

		#region Crop

		/// <summary>
		/// Crops an image. Always saves the center.
		/// </summary>
		/// <param name="bitmap">The bitmap of the image.</param>
		/// <param name="width">The wanted width.</param>
		/// <param name="height">The wanted height.</param>
		/// <returns>The resized bitmap.</returns>
		public static Bitmap Crop(this Bitmap bitmap, int width, int height)
		{
			// First, resize, so we'll crop only the needed parts.
			var resizedImage = Resize(bitmap, width, height, true, true);

            // Calculate the cropping location.
		    int widthCropping = (resizedImage.Width > width)
		        ? ((resizedImage.Width/2) - (width/2))
		        : 0;

            int heightCropping = (resizedImage.Height > height)
                ? ((resizedImage.Height / 2) - (height / 2))
                : 0;

			// Crop into new bitmap.
			var newImage = resizedImage.Clone(new Rectangle(
				widthCropping, heightCropping,
				width, height), resizedImage.PixelFormat);

			// Return the resized image.
			return (newImage);
		}

        #endregion

	    #region Exif

	    private const int exifOrientationID = 0x112; //274

	    public static void FixExifRotatation(Bitmap img)
	    {
	        if (!img.PropertyIdList.Contains(exifOrientationID))
	            return;

	        var prop = img.GetPropertyItem(exifOrientationID);
	        int val = BitConverter.ToUInt16(prop.Value, 0);
	        var rot = RotateFlipType.RotateNoneFlipNone;

	        if (val == 3 || val == 4)
	            rot = RotateFlipType.Rotate180FlipNone;
	        else if (val == 5 || val == 6)
	            rot = RotateFlipType.Rotate90FlipNone;
	        else if (val == 7 || val == 8)
	            rot = RotateFlipType.Rotate270FlipNone;

	        if (val == 2 || val == 4 || val == 5 || val == 7)
	            rot |= RotateFlipType.RotateNoneFlipX;

	        if (rot != RotateFlipType.RotateNoneFlipNone)
	            img.RotateFlip(rot);
	    }


	    #endregion
        
        public static Stream ToStream(this Bitmap bitmap, ImageFormat imageFormat)
		{
			var memStream = new MemoryStream();
			
			bitmap.Save(memStream, imageFormat);
			memStream.Position = 0;

			return memStream;
		}

		public static bool IsImageStream(this Stream stream)
		{
			bool result;

			try
			{
				var bitmap = new Bitmap(stream);

				result = true;
				stream.Position = 0;
			}
			catch
			{
				result = false;
			}

			return result;
		}
	}
}
