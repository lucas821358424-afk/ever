namespace EverInspection.Client.Models
{
    public sealed class PermissionInfo
    {
        public string UserId { get; set; }
        public string TemplateId { get; set; }
        public bool FormVisible { get; set; }
        public bool FormFill { get; set; }
        public bool HistoryEdit { get; set; }
        public bool DraftDelete { get; set; }
    }
}
