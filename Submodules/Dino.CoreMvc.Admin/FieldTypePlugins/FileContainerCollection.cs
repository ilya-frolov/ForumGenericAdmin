using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dino.CoreMvc.Admin.Attributes;
using Newtonsoft.Json;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Represents a collection of file containers organized by platform
    /// </summary>
    public class FileContainerCollection
    {
        /// <summary>
        /// Dictionary of platforms to file containers
        /// </summary>

        private Dictionary<Platforms, List<FileContainer>> _platformFiles;

        public Dictionary<Platforms, List<FileContainer>> PlatformFiles
        {
            get
            {
                return _platformFiles;
            }
            set
            {
                _platformFiles = value;
            }
        }

        /// <summary>
        /// Full structured variant data from PictureVariant processing.
        /// Structure: Platform -> VariantName -> Format -> Path
        /// Null when no PictureVariant attributes are configured (legacy behavior).
        /// </summary>
        [JsonProperty("variantData", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> VariantData { get; set; }

        /// <summary>
        /// Gets all file containers across all platforms
        /// </summary>
        public IEnumerable<FileContainer> AllFiles()
        {
            return PlatformFiles.Values.SelectMany(v => v);
        }

        /// <summary>
        /// Creates a new instance of FileContainerCollection
        /// </summary>
        public FileContainerCollection()
        {
            _platformFiles = new Dictionary<Platforms, List<FileContainer>>();
            // Initialize with empty collections for each platform
            foreach (Platforms platform in Enum.GetValues(typeof(Platforms)))
            {
                PlatformFiles[platform] = new List<FileContainer>();
            }
        }

        /// <summary>
        /// Gets files for a specific platform
        /// </summary>
        /// <param name="platform">The platform to get files for</param>
        /// <returns>List of file containers for the platform</returns>
        public List<FileContainer> GetFiles(Platforms platform)
        {
            if (PlatformFiles.TryGetValue(platform, out var files))
            {
                return files;
            }

            return new List<FileContainer>();
        }

        /// <summary>
        /// Adds a file to a specific platform
        /// </summary>
        /// <param name="platform">The platform to add the file to</param>
        /// <param name="file">The file container to add</param>
        public void AddFile(Platforms platform, FileContainer file)
        {
            if (file == null)
                return;

            // Add to specific platform
            if (!PlatformFiles.ContainsKey(platform))
            {
                PlatformFiles[platform] = new List<FileContainer>();
            }

            if (!PlatformFiles[platform].Contains(file))
            {
                PlatformFiles[platform].Add(file);
            }
        }

        /// <summary>
        /// Marks a file for deletion across all platforms
        /// </summary>
        /// <param name="fileId">The ID of the file to mark for deletion</param>
        public void MarkForDeletion(string Path)
        {
            if (string.IsNullOrEmpty(Path))
                return;

            foreach (var platform in PlatformFiles.Keys)
            {
                var file = PlatformFiles[platform].FirstOrDefault(f => f.Path == Path);
                if (file != null)
                {
                    file.IsMarkedForDeletion = true;
                }
            }
        }

        /// <summary>
        /// Marks a file for deletion on a specific platform
        /// </summary>
        /// <param name="platform">The platform</param>
        /// <param name="fileId">The ID of the file to mark for deletion</param>
        public void MarkForDeletion(Platforms platform, string Path)
        {
            if (string.IsNullOrEmpty(Path))
                return;

            if (PlatformFiles.TryGetValue(platform, out var files))
            {
                var file = files.FirstOrDefault(f => f.Path == Path);
                if (file != null)
                {
                    file.IsMarkedForDeletion = true;
                }
            }
        }

        /// <summary>
        /// Removes a file from a specific platform
        /// </summary>
        /// <param name="platform">The platform to remove the file from</param>
        /// <param name="fileId">The ID of the file to remove</param>
        public void RemoveFile(Platforms platform, string Path)
        {
            if (string.IsNullOrEmpty(Path))
                return;

            if (PlatformFiles.ContainsKey(platform))
            {
                PlatformFiles[platform].RemoveAll(f => f.Path == Path);
            }
        }

        /// <summary>
        /// Adds a pending upload to a specific platform
        /// </summary>
        /// <param name="platform">The platform</param>
        /// <param name="fileName">The file name</param>
        /// <param name="stream">The file stream</param>
        /// <param name="contentType">The content type</param>
        /// <returns>The created file container</returns>
        public FileContainer AddPendingUpload(Platforms platform, string fileName, Stream stream, string contentType = null)
        {
            var file = new FileContainer
            {
                Name = fileName,
                PendingUpload = new PendingFileUpload
                {
                    FileName = fileName,
                    Stream = stream,
                    ContentType = contentType ?? "application/octet-stream"
                }
            };

            AddFile(platform, file);
            return file;
        }

        /// <summary>
        /// Clears all files from the collection
        /// </summary>
        public void Clear()
        {
            foreach (var platform in PlatformFiles.Keys)
            {
                PlatformFiles[platform].Clear();
            }
        }
    }

    /// <summary>
    /// Represents a file container with metadata
    /// </summary>
    public class FileContainer
    {
        /// <summary>
        /// Gets or sets the file name
        /// </summary
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the file path or URL
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets whether the file is marked for deletion
        /// </summary>
        [JsonProperty("isMarkedForDeletion")]
        public bool IsMarkedForDeletion { get; set; }

        /// <summary>
        /// Gets or sets pending upload information
        /// </summary>
        public PendingFileUpload PendingUpload { get; set; }

        /// <summary>
        /// Gets the file extension
        /// </summary>
        public string Extension => System.IO.Path.GetExtension(Path ?? Name);

        /// <summary>
        /// Creates a new instance of FileContainer
        /// </summary>
        public FileContainer()
        {
        }

        /// <summary>
        /// Creates a new instance of FileContainer with the specified path
        /// </summary>
        /// <param name="path">The file path or URL</param>
        public FileContainer(string path) : this()
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is FileContainer other)
            {
                return Path == other.Path;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return Path?.GetHashCode() ?? 0;
        }
    }


    /// <summary>
    /// Represents a file container with metadata
    /// </summary>
    public class MiniFileContainerForDB
    {
        /// <summary>
        /// Gets or sets the file name
        /// </summary
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the file path or URL
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Creates a new instance of FileContainer
        /// </summary>
        public MiniFileContainerForDB()
        {
        }

        /// <summary>
        /// Creates a new instance of FileContainer with the specified path
        /// </summary>
        /// <param name="path">The file path or URL</param>
        public MiniFileContainerForDB(string path) : this()
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is FileContainer other)
            {
                return Path == other.Path;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return Path?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Represents a pending file upload
    /// </summary>
    public class PendingFileUpload
    {
        /// <summary>
        /// Gets or sets the file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file stream
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Gets or sets the content type
        /// </summary>
        public string ContentType { get; set; }
    }
}