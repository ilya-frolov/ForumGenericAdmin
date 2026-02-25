using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dino.Core.AdminBL.Settings
{
    /// <summary>
    /// Base interface for all settings in the system
    /// </summary>
    public interface IAdminBaseSettings
    {
        /// <summary>
        /// The class name of the settings implementation
        /// </summary>
        string ClassName { get; set; }
        
        /// <summary>
        /// Display name for the settings
        /// </summary>
        string Name { get; set; }
    }
}
