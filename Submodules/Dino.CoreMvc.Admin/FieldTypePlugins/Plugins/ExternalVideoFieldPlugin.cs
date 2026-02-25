using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dino.CoreMvc.Admin.Attributes;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for external video field types
    /// </summary>
    public class ExternalVideoFieldPlugin : BaseFieldTypePlugin<AdminFieldExternalVideoAttribute, string>
    {
        // Regex pattern for YouTube video IDs
        private static readonly Regex YouTubeRegex = new Regex(
            @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Regex pattern for Vimeo video IDs
        private static readonly Regex VimeoRegex = new Regex(
            @"(?:vimeo\.com\/(?:channels\/(?:\w+\/)?|groups\/[^\/]+\/videos\/|album\/\d+\/video\/|)(\d+)(?:$|\/|\?))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "ExternalVideo";

        public ExternalVideoFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates an external video field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(string value, PropertyInfo property)
        {
            // First validate using base implementation (handles required fields)
            var baseResult = base.Validate(value, property);
            if (!baseResult.IsValid)
                return baseResult;

            // No further validation if value is null or empty
            if (string.IsNullOrEmpty(value))
                return (true, new List<string>());

            var errorMessages = new List<string>();
            var fieldAttribute = property.GetCustomAttribute<AdminFieldExternalVideoAttribute>();

            // Validate based on the video source type
            switch (fieldAttribute.SourceType)
            {
                case ExternalVideoSourceType.YouTube:
                    if (!IsValidYouTubeVideo(value))
                    {
                        errorMessages.Add($"Field '{property.Name}' must contain a valid YouTube video ID or URL");
                    }
                    break;

                case ExternalVideoSourceType.Vimeo:
                    if (!IsValidVimeoVideo(value))
                    {
                        errorMessages.Add($"Field '{property.Name}' must contain a valid Vimeo video ID or URL");
                    }
                    break;
                    
                case ExternalVideoSourceType.AutoDetect:
                    // For "AutoDetect" type, check if it matches any of the supported video types
                    if (!IsValidYouTubeVideo(value) && !IsValidVimeoVideo(value))
                    {
                        errorMessages.Add($"Field '{property.Name}' must contain a valid YouTube or Vimeo video ID or URL");
                    }
                    break;
            }

            return (errorMessages.Count == 0, errorMessages);
        }

        /// <summary>
        /// Prepares a typed value for database storage
        /// </summary>
        protected override object PrepareTypedValueForDb(string value, PropertyInfo property)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var fieldAttribute = property.GetCustomAttribute<AdminFieldExternalVideoAttribute>();

            // Extract just the video ID based on the source type
            switch (fieldAttribute.SourceType)
            {
                case ExternalVideoSourceType.YouTube:
                    return ExtractYouTubeVideoId(value);

                case ExternalVideoSourceType.Vimeo:
                    return ExtractVimeoVideoId(value);
                    
                case ExternalVideoSourceType.AutoDetect:
                    // For "AutoDetect" type, auto-detect the video type
                    if (IsValidYouTubeVideo(value))
                    {
                        return ExtractYouTubeVideoId(value);
                    }
                    else if (IsValidVimeoVideo(value))
                    {
                        return ExtractVimeoVideoId(value);
                    }
                    break;
            }
            
            // Default case - return the original value
            return value;
        }

        /// <summary>
        /// Gets the detected video source type for an input
        /// </summary>
        public ExternalVideoSourceType DetectVideoSourceType(string input)
        {
            if (string.IsNullOrEmpty(input))
                return ExternalVideoSourceType.YouTube; // Default
                
            if (IsValidYouTubeVideo(input))
                return ExternalVideoSourceType.YouTube;
                
            if (IsValidVimeoVideo(input))
                return ExternalVideoSourceType.Vimeo;
                
            // Default to YouTube if we can't determine
            return ExternalVideoSourceType.YouTube;
        }

        #region Helper Methods

        /// <summary>
        /// Determines if a string is a valid YouTube video ID or URL
        /// </summary>
        private bool IsValidYouTubeVideo(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
                
            // Check if it's just a valid YouTube ID (11 characters)
            if (input.Length == 11 && !input.Contains(" ") && !input.Contains("/") && 
                input.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                return true;

            // Otherwise, check if it's a valid YouTube URL with a video ID
            return YouTubeRegex.IsMatch(input);
        }

        /// <summary>
        /// Determines if a string is a valid Vimeo video ID or URL
        /// </summary>
        private bool IsValidVimeoVideo(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
                
            // Check if it's just a numeric Vimeo ID
            if (input.Length > 0 && input.All(char.IsDigit))
                return true;

            // Otherwise, check if it's a valid Vimeo URL with a video ID
            return VimeoRegex.IsMatch(input);
        }

        /// <summary>
        /// Extracts the YouTube video ID from a URL or returns the ID if already just the ID
        /// </summary>
        private string ExtractYouTubeVideoId(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            // If it's already just the ID (11 characters)
            if (input.Length == 11 && !input.Contains(" ") && !input.Contains("/") && 
                input.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                return input;

            // Try to extract from URL
            var match = YouTubeRegex.Match(input);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            // If we couldn't extract an ID, return the original input
            return input;
        }

        /// <summary>
        /// Extracts the Vimeo video ID from a URL or returns the ID if already just the ID
        /// </summary>
        private string ExtractVimeoVideoId(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            // If it's already just the ID (numeric)
            if (input.Length > 0 && input.All(char.IsDigit))
                return input;

            // Try to extract from URL
            var match = VimeoRegex.Match(input);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            // If we couldn't extract an ID, return the original input
            return input;
        }

        #endregion
    }
} 