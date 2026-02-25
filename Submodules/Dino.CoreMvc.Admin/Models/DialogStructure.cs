using Dino.CoreMvc.Admin.Models.Admin;

namespace Dino.CoreMvc.Admin.Models
{
    public class DialogStructure
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string OkButtonText { get; set; }
        public string CancelButtonText { get; set; }
        public Type? FormStructureType { get; set; }
        public FormNodeContainer Structure { get; set; }
    }
}
