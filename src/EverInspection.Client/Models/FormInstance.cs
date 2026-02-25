using System;
using System.Collections.Generic;

namespace EverInspection.Client.Models
{
    public sealed class FormInstance
    {
        public string FormInstanceId { get; set; }
        public string TemplateId { get; set; }
        public string TemplateVersion { get; set; }
        public string OperatorId { get; set; }
        public FormStatus Status { get; set; }
        public bool HasOutOfSpec { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<FormCellValue> Values { get; set; } = new List<FormCellValue>();
    }

    public sealed class FormCellValue
    {
        public int RowIndex { get; set; }
        public string ValueText { get; set; }
        public bool IsOutOfSpec { get; set; }
    }
}
