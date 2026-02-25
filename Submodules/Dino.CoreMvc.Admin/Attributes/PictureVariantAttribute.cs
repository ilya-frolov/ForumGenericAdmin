using System;

namespace Dino.CoreMvc.Admin.Attributes
{
    /// <summary>
    /// Defines an image variant to be generated when a picture is saved.
    /// Multiple variants can be stacked on a single picture property.
    /// Each variant specifies output formats, optional resize dimensions, and optional platform restrictions.
    /// 
    /// If no PictureVariant attributes are present on a property, the existing (legacy) behavior is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class PictureVariantAttribute : Attribute
    {
        /// <summary>
        /// Display name of the variant (e.g. "Original", "Thumbnail").
        /// Used as a key in the generated JSON structure.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The platforms this variant applies to.
        /// When set to <c>(Platforms)0</c> (the default), the variant inherits
        /// the platforms defined on the parent <see cref="AdminFieldPictureAttribute"/>.
        /// </summary>
        public Platforms Platforms { get; set; }

        /// <summary>
        /// Output formats to generate for this variant (e.g. "png", "webp", "jpg").
        /// </summary>
        public string[] Formats { get; set; }

        /// <summary>
        /// Target width in pixels. 0 = keep original width (or scale proportionally if Height is set).
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Target height in pixels. 0 = keep original height (or scale proportionally if Width is set).
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Defines an image variant to generate on save.
        /// </summary>
        /// <param name="formats">Output formats (e.g. "png", "webp").</param>
        /// <param name="name">Variant name. Defaults to "Original".</param>
        /// <param name="platforms">
        /// Platforms this variant applies to. Use <c>(Platforms)0</c> (default) to inherit from parent attribute.
        /// </param>
        /// <param name="width">Target width. 0 = keep original / proportional.</param>
        /// <param name="height">Target height. 0 = keep original / proportional.</param>
        public PictureVariantAttribute(
            string[] formats,
            string name = "Original",
            Platforms platforms = 0,
            int width = 0,
            int height = 0)
        {
            Formats = formats ?? throw new ArgumentNullException(nameof(formats));
            Name = name;
            Platforms = platforms;
            Width = width;
            Height = height;
        }
    }
}
