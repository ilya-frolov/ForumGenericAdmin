using System;
using System.Collections.Generic;
using System.Text;

namespace Dino.Common.Files
{
    public interface IWebFilePathResolver
    {
        string Resolve(string path);

        string ResolveUpload(string path);
    }
}
