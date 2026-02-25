using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Drawing.Imaging;

namespace Dino.Infra.Images
{
    public static class ImageResizer
    {
        // Consts.
        public static PngCompressionLevel PNG_COMPRESSION_LEVEL = PngCompressionLevel.Level9;

        /// <summary>
        /// Fixes an image according to given properties. Also fixes the EXIF rotation.
        /// </summary>
        /// <param name="image">The image to manipulate.</param>
        /// <param name="width">The desired width.</param>
        /// <param name="height">The desired height.</param>
        /// <param name="crop">Should we crop the image. If true, the 'resize' property will be ignored.</param>
        /// <param name="resize">Should we resize the image. Only available if 'crop' property is false.</param>
        /// <param name="keepRatio">Should we keep the ratio when resizing. Only available if 'crop' is false and 'resize' is true.</param>
        /// <param name="forceJpg">Do we want to force JPG encoding.</param>
        /// <param name="jpgQuality">The JPG quality for the encoding. Only available if 'forceJpg' is true.</param>
        /// <returns>The manipulated image, as a stream (initialized to position 0).</returns>
        public static Stream FixImageSizingToStream(byte[] image, int width, int height, bool crop, bool resize, bool keepRatio,
            bool forceJpg = false, int jpgQuality = 85)
        {
            IImageFormat format;
            var img = FixImageSizing(image, width, height, crop, resize, keepRatio, out format);

            MemoryStream stream = new MemoryStream();

            if (forceJpg)
            {
                img.Save(stream, new JpegEncoder
                {
                    Quality = jpgQuality
                });
            }
            else if (format is PngFormat)
            {

                img.Save(stream, new PngEncoder
                {
                    CompressionLevel = PNG_COMPRESSION_LEVEL,
                    //FilterMethod = PngFilterMethod.None,
                    //BitDepth = PngBitDepth.Bit8,
                    //Quantizer = new SixLabors.ImageSharp.Processing.Processors.Quantization.OctreeQuantizer(),
                    //CompressionLevel = 9
                });
            }
            else
            {
                img.Save(stream, format);
            }

            stream.Position = 0;


            return stream;
        }

        /// <summary>
        /// Fixes an image according to given properties. Also fixes the EXIF rotation.
        /// </summary>
        /// <param name="image">The image to manipulate.</param>
        /// <param name="destPath">The destination path, where we want to save the image.</param>
        /// <param name="width">The desired width.</param>
        /// <param name="height">The desired height.</param>
        /// <param name="crop">Should we crop the image. If true, the 'resize' property will be ignored.</param>
        /// <param name="resize">Should we resize the image. Only available if 'crop' property is false.</param>
        /// <param name="keepRatio">Should we keep the ratio when resizing. Only available if 'crop' is false and 'resize' is true.</param>
        /// <param name="forceJpg">Do we want to force JPG encoding.</param>
        /// <param name="jpgQuality">The JPG quality for the encoding. Only available if 'forceJpg' is true.</param>
        public static void FixImageSizingToFile(byte[] image, string destPath, int width, int height, bool crop, bool resize, bool keepRatio,
            bool forceJpg = false, int jpgQuality = 85)
        {
            IImageFormat format;
            var img = FixImageSizing(image, width, height, crop, resize, keepRatio, out format);

            if (forceJpg)
            {
                img.Save(destPath, new JpegEncoder
                {
                    Quality = jpgQuality
                });
            }
            else
            {
                img.Save(destPath);
            }
        }

        /// <summary>
        /// Fixes an image according to given properties. Also fixes the EXIF rotation.
        /// </summary>
        /// <param name="image">The image to manipulate.</param>
        /// <param name="width">The desired width.</param>
        /// <param name="height">The desired height.</param>
        /// <param name="crop">Should we crop the image. If true, the 'resize' property will be ignored.</param>
        /// <param name="resize">Should we resize the image. Only available if 'crop' property is false.</param>
        /// <param name="keepRatio">Should we keep the ratio when resizing. Only available if 'crop' is false and 'resize' is true.</param>
        /// <param name="forceJpg">Do we want to force JPG encoding.</param>
        /// <param name="jpgQuality">The JPG quality for the encoding. Only available if 'forceJpg' is true.</param>
        public static MemoryStream FixImageSizingToMemoryStream(byte[] image, int width, int height, bool crop, bool resize, bool keepRatio,
            bool forceJpg = false, int jpgQuality = 85)
        {
            IImageFormat format;
            var img = FixImageSizing(image, width, height, crop, resize, keepRatio, out format);

            MemoryStream stream = new MemoryStream();

            if (forceJpg)
            {
                img.Save(stream, new JpegEncoder
                {
                    Quality = jpgQuality
                });
            }
            else
            {
                img.Save(stream, format);
            }

            return stream;
        }

        /// <summary>
        /// Fixes an image according to given properties. Also fixes the EXIF rotation.
        /// </summary>
        /// <param name="image">The image to manipulate.</param>
        /// <param name="width">The desired width.</param>
        /// <param name="height">The desired height.</param>
        /// <param name="crop">Should we crop the image. If true, the 'resize' property will be ignored.</param>
        /// <param name="resize">Should we resize the image. Only available if 'crop' property is false.</param>
        /// <param name="keepRatio">Should we keep the ratio when resizing. Only available if 'crop' is false and 'resize' is true.</param>
        /// <param name="format">Out: The original image format.</param>
        /// <returns>The manipulated image.</returns>
        public static Image FixImageSizing(byte[] image, int width, int height, bool crop, bool resize, bool keepRatio, out IImageFormat format)
        {
            Image img = Image<Rgba32>.Load(image, out format);

            // First, always auto-orient to fix Exif.
            img.Mutate(x => x.AutoOrient());

            // Crop or resize if needed.
            if (crop || resize)
            {
                ResizeOptions options = new ResizeOptions
                {
                    Size = new Size(width, height)
                };

                if (crop)
                {
                    options.Mode = ResizeMode.Crop;
                }
                else
                {
                    options.Mode = keepRatio ? ResizeMode.Min : ResizeMode.Stretch;
                }

                img.Mutate(x => x.Resize(options));
            }

            return img;
        }

        /// <summary>
        /// Resizes (optionally) and converts a source image to the specified output format.
        /// Used by the generic image variant generation pipeline.
        /// </summary>
        /// <param name="source">Raw bytes of the source image.</param>
        /// <param name="width">Target width. 0 = keep original (or scale proportionally if height > 0).</param>
        /// <param name="height">Target height. 0 = keep original (or scale proportionally if width > 0).</param>
        /// <param name="targetFormat">Output format: "png", "webp", "jpg"/"jpeg", "gif".</param>
        /// <param name="jpgQuality">JPEG quality (1-100). Used only when targetFormat is jpg/jpeg.</param>
        /// <returns>A MemoryStream containing the processed image, positioned at 0.</returns>
        public static MemoryStream ResizeAndConvert(byte[] source, int width, int height, string targetFormat, int jpgQuality = 85)
        {
            IImageFormat originalFormat;
            Image img = Image<Rgba32>.Load(source, out originalFormat);

            img.Mutate(x => x.AutoOrient());

            bool needsResize = width > 0 || height > 0;
            if (needsResize)
            {
                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(width > 0 ? width : 0, height > 0 ? height : 0),
                    Mode = ResizeMode.Max
                };
                img.Mutate(x => x.Resize(resizeOptions));
            }

            var stream = new MemoryStream();
            var encoder = GetEncoderForFormat(targetFormat, jpgQuality);
            img.Save(stream, encoder);
            img.Dispose();
            stream.Position = 0;
            return stream;
        }

        private static IImageEncoder GetEncoderForFormat(string format, int jpgQuality = 85)
        {
            switch (format?.ToLowerInvariant())
            {
                case "png":
                    return new PngEncoder { CompressionLevel = PNG_COMPRESSION_LEVEL };
                case "webp":
                    return new WebpEncoder { Quality = 80 };
                case "jpg":
                case "jpeg":
                    return new JpegEncoder { Quality = jpgQuality };
                case "gif":
                    return new GifEncoder();
                default:
                    return new PngEncoder { CompressionLevel = PNG_COMPRESSION_LEVEL };
            }
        }

        /// <summary>
        /// Returns the MIME content type for a given format string.
        /// </summary>
        public static string GetContentTypeForFormat(string format)
        {
            switch (format?.ToLowerInvariant())
            {
                case "png": return "image/png";
                case "webp": return "image/webp";
                case "jpg":
                case "jpeg": return "image/jpeg";
                case "gif": return "image/gif";
                default: return "image/png";
            }
        }
    }
}
