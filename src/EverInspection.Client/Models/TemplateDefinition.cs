using System;
using System.Collections.Generic;

namespace EverInspection.Client.Models
{
    public sealed class TemplateDefinition
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Version { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool EnablePanelId { get; set; }
        public bool EnableMachineType { get; set; }
        public List<HeaderFieldDefinition> HeaderFields { get; set; } = new List<HeaderFieldDefinition>();
        public List<TemplateRowDefinition> Rows { get; set; } = new List<TemplateRowDefinition>();
    }

    public sealed class HeaderFieldDefinition
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public bool Required { get; set; }
    }

    public sealed class TemplateRowDefinition
    {
        public string Process { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public bool Required { get; set; }
        public FieldType FieldType { get; set; }
    }
}
