using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dino.CoreMvc.Admin.Models.Exceptions
{
    /// <summary>
    /// Custom exception for missing container/tab end markers
    /// </summary>
    public class MissingEndContainerException : Exception
    {
        public MissingEndContainerException(string message) : base(message) { }
    }
}
