namespace Dino.CoreMvc.Admin.Models.Admin
{
    public class AdminHomeData
    {
        public bool ShowDashboard { get; set; }
        public bool ShowGlobalStatistics { get; set; }
        public string SiteBaseHref { get; set; }
        public string UserName { get; set; }
        public List<AdminSegment> Segments { get; set; }
        public List<AdminSettingsSegment> Settings { get; set; }
    }
}
