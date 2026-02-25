using System.IO;

namespace Dino.Common.Files
{
    public class FileUploadRequest
    {
        public string FileName { get; set; }
        public Stream FileStream { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public bool DeleteOld { get; set; }
    }
}
