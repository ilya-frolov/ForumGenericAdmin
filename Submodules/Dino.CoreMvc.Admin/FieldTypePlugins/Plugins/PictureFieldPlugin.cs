using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dino.Core.AdminBL.Contracts;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Contracts;
using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base;
using Dino.CoreMvc.Admin.Logic.Helpers;
using Dino.Infra.Files.Uploaders;
using Dino.Infra.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dino.CoreMvc.Admin.FieldTypePlugins.Plugins
{
    /// <summary>
    /// Plugin for handling picture fields with image-specific constraints.
    /// When <see cref="PictureVariantAttribute"/> attributes are present on the property,
    /// this plugin generates all required image variants (platform × size × format)
    /// on save and stores a structured JSON in the database.
    /// </summary>
    public class PictureFieldPlugin : FileFieldBasePlugin<AdminFieldPictureAttribute>
    {
        /// <summary>
        /// Gets the field type this plugin handles.
        /// </summary>
        public override string FieldType => "Picture";

        private readonly IServiceProvider _serviceProvider;

        public PictureFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Prepares a picture value for database storage.
        /// If <see cref="PictureVariantAttribute"/> definitions exist on the property,
        /// generates all image variants and returns structured JSON.
        /// Otherwise falls back to the base file-field behavior.
        /// </summary>
        protected override object PrepareTypedValueForDb(object value, PropertyInfo property)
        {
            if (value == null)
                return null;

            // Check for PictureVariant attribute definitions
            var variantAttrs = property.GetCustomAttributes<PictureVariantAttribute>().ToArray();

            // No variants defined → fall back to base behavior (backward compatible)
            if (variantAttrs.Length == 0)
                return base.PrepareTypedValueForDb(value, property);

            try
            {
                // Parse the incoming collection
                FileContainerCollection? collection = value switch
                {
                    string json when !string.IsNullOrEmpty(json) =>
                        JsonConvert.DeserializeObject<FileContainerCollection>(json),
                    FileContainerCollection map => map,
                    _ => null
                };

                if (collection == null)
                    return base.PrepareTypedValueForDb(value, property);

                var pictureAttr = property.GetCustomAttribute<AdminFieldPictureAttribute>();
                var supportedPlatforms = pictureAttr?.Platforms ?? (Platforms.Desktop | Platforms.Mobile);

                // Process file uploads and deletions (handles the source image upload)
                ProcessFileOperations(collection);

                var fileUploader = GetFileUploader();
                var apiConfig = _serviceProvider.GetService<IOptions<BaseApiConfig>>()?.Value;
                var uploadFolderName = apiConfig?.UploadsFolder ?? "uploadsDefault";

                // Build structured result: Platform → VariantName → Format → Path
                var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                foreach (Platforms platform in Enum.GetValues(typeof(Platforms)))
                {
                    // Skip platforms not supported by this property
                    if ((supportedPlatforms & platform) == 0)
                        continue;

                    var files = collection.GetFiles(platform);
                    var sourceFile = files.FirstOrDefault(f => !f.IsMarkedForDeletion);

                    if (sourceFile == null || string.IsNullOrEmpty(sourceFile.Path))
                        continue;

                    // Read source image bytes from storage
                    byte[] sourceBytes = ReadSourceBytes(fileUploader, sourceFile.Path, uploadFolderName);
                    if (sourceBytes == null || sourceBytes.Length == 0)
                        continue;

                    // Determine the base filename for variant files
                    string relativePath = StripToRelativePath(sourceFile.Path, uploadFolderName);
                    string sourceNameWithoutExt = Path.GetFileNameWithoutExtension(relativePath);

                    var platformVariants = new Dictionary<string, Dictionary<string, string>>();

                    foreach (var variant in variantAttrs)
                    {
                        // Check if this variant applies to the current platform
                        // Platforms == 0 means "inherit all from parent attribute"
                        var variantPlatforms = variant.Platforms != 0
                            ? variant.Platforms
                            : supportedPlatforms;

                        if ((variantPlatforms & platform) == 0)
                            continue;

                        var formatPaths = new Dictionary<string, string>();

                        foreach (var format in variant.Formats)
                        {
                            string normalizedFormat = format.ToLowerInvariant();

                            // Build a unique variant filename
                            string sizeSuffix = (variant.Width > 0 || variant.Height > 0)
                                ? $"_{variant.Width}x{variant.Height}"
                                : "";
                            string variantFileName =
                                $"{sourceNameWithoutExt}_{variant.Name}{sizeSuffix}.{normalizedFormat}";

                            // Generate the image variant (resize + format conversion)
                            using var variantStream = ImageResizer.ResizeAndConvert(
                                sourceBytes, variant.Width, variant.Height, normalizedFormat);

                            // Upload the variant file
                            string contentType = ImageResizer.GetContentTypeForFormat(normalizedFormat);
                            var uploadTask = new FileUploadTask(
                                variantStream,
                                variantFileName,
                                isCustomPath: false,
                                contentType: contentType);

                            string uploadedPath = fileUploader.UploadFile(uploadTask);
                            formatPaths[normalizedFormat] = uploadedPath;
                        }

                        if (formatPaths.Count > 0)
                        {
                            platformVariants[variant.Name] = formatPaths;
                        }
                    }

                    if (platformVariants.Count > 0)
                    {
                        // Preserve the original source path for re-processing / debugging
                        platformVariants["_source"] = new Dictionary<string, string>
                        {
                            { "path", sourceFile.Path },
                            { "name", sourceFile.Name ?? System.IO.Path.GetFileName(sourceFile.Path) ?? "" }
                        };

                        result[platform.ToString()] = platformVariants;
                    }
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PictureFieldPlugin.PrepareTypedValueForDb: {ex.Message}");

                // Fall back to base behavior on error
                return base.PrepareTypedValueForDb(value, property);
            }
        }

        /// <summary>
        /// Prepares a database value for model use.
        /// Handles both the new structured variant JSON format and the legacy flat format.
        /// New format: { "Desktop": { "Original": { "png": "path", "webp": "path" } } }
        /// Legacy format: { "Desktop": [{ "name": "...", "path": "...", "size": ... }] }
        /// </summary>
        protected override object PrepareTypedValueForModel(object dbValue, PropertyInfo property)
        {
            if (dbValue == null)
                return null;

            try
            {
                if (dbValue is FileContainerCollection map)
                    return map;

                if (dbValue is string json && !string.IsNullOrEmpty(json))
                {
                    // Not JSON at all → delegate to base (handles single-path strings)
                    if (!json.TrimStart().StartsWith("{") && !json.TrimStart().StartsWith("["))
                        return base.PrepareTypedValueForModel(dbValue, property);

                    // Try to detect the new structured variant format
                    // by checking if the first platform value is an object (not an array).
                    var jObj = JObject.Parse(json);
                    var firstPlatformToken = jObj.Properties().FirstOrDefault()?.Value;

                    if (firstPlatformToken != null && firstPlatformToken.Type == JTokenType.Object)
                    {
                        // New structured variant format detected
                        return ParseStructuredVariantJson(jObj, property);
                    }

                    // Else: it's the legacy flat format → delegate to base
                    return base.PrepareTypedValueForModel(dbValue, property);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PictureFieldPlugin.PrepareTypedValueForModel: {ex.Message}");
                return dbValue;
            }

            return base.PrepareTypedValueForModel(dbValue, property);
        }

        /// <summary>
        /// Parses the new structured variant JSON into a FileContainerCollection
        /// for admin UI display purposes.
        /// Uses the first variant's first format as the display image per platform,
        /// and populates the full VariantData dictionary for frontend consumption.
        /// </summary>
        private FileContainerCollection ParseStructuredVariantJson(JObject jObj, PropertyInfo property)
        {
            var apiConfig = _serviceProvider.GetService<IOptions<BaseApiConfig>>()?.Value;
            var blConfig = _serviceProvider.GetService<IOptions<BaseBlConfig>>()?.Value;

            var collection = new FileContainerCollection();

            // Build the full variant data structure (excluding _source metadata)
            var variantData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            foreach (var platformProp in jObj.Properties())
            {
                if (!Enum.TryParse<Platforms>(platformProp.Name, out var platform))
                    continue;

                if (platformProp.Value is not JObject variants)
                    continue;

                // Build variant data for this platform (skip internal _source key)
                var platformVariantData = new Dictionary<string, Dictionary<string, string>>();
                JObject firstRealVariant = null;

                foreach (var variantProp in variants.Properties())
                {
                    if (variantProp.Name.StartsWith("_"))
                        continue; // Skip metadata keys like _source

                    if (variantProp.Value is not JObject variantFormats)
                        continue;

                    if (firstRealVariant == null)
                        firstRealVariant = variantFormats;

                    var formatPaths = new Dictionary<string, string>();
                    foreach (var formatProp in variantFormats.Properties())
                    {
                        var rawPath = formatProp.Value?.ToString();
                        if (string.IsNullOrEmpty(rawPath))
                            continue;

                        // Normalize path with full URL for frontend display
                        var normalizedPath = (apiConfig != null && blConfig != null)
                            ? PathHelpers.GetUploadsFullPath(apiConfig, blConfig, rawPath)
                            : rawPath;

                        formatPaths[formatProp.Name] = normalizedPath;
                    }

                    if (formatPaths.Count > 0)
                        platformVariantData[variantProp.Name] = formatPaths;
                }

                if (platformVariantData.Count > 0)
                    variantData[platform.ToString()] = platformVariantData;

                // Use the first real variant's first format path as the display image
                if (firstRealVariant == null)
                    continue;

                var firstFormatPath = firstRealVariant.Properties().FirstOrDefault()?.Value?.ToString();
                if (string.IsNullOrEmpty(firstFormatPath))
                    continue;

                var displayPath = (apiConfig != null && blConfig != null)
                    ? PathHelpers.GetUploadsFullPath(apiConfig, blConfig, firstFormatPath)
                    : firstFormatPath;

                var fileContainer = new FileContainer
                {
                    Path = displayPath,
                    Name = System.IO.Path.GetFileName(firstFormatPath)
                };

                collection.AddFile(platform, fileContainer);
            }

            // Attach the full variant data for frontend consumption
            if (variantData.Count > 0)
                collection.VariantData = variantData;

            return collection;
        }

        /// <summary>
        /// Reads the source image bytes from storage.
        /// Handles both relative paths and full URLs by stripping the upload folder prefix.
        /// </summary>
        private byte[]? ReadSourceBytes(IFileUploader fileUploader, string path, string uploadFolderName)
        {
            try
            {
                string relativePath = StripToRelativePath(path, uploadFolderName);
                using var stream = fileUploader.GetFileStream(relativePath);
                if (stream == null)
                    return null;

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading source image from storage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Strips a full URL or prefixed path down to its storage-relative form.
        /// e.g. "https://cdn.example.com/uploads/hero_123.webp" → "hero_123.webp"
        /// </summary>
        private static string StripToRelativePath(string path, string uploadFolderName)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Try forward-slash split (URLs / Linux paths)
            var parts = path.Split(new[] { $"/{uploadFolderName}/" }, StringSplitOptions.None);
            if (parts.Length == 2)
                return parts[1];

            // Try backslash split (Windows paths)
            parts = path.Split(new[] { $"\\{uploadFolderName}\\" }, StringSplitOptions.None);
            if (parts.Length == 2)
                return parts[1];

            // Already relative
            return path;
        }
    }
}
