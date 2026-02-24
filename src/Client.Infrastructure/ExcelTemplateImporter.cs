using Client.Domain;
using ClosedXML.Excel;

namespace Client.Infrastructure;

public sealed class ExcelTemplateImporter
{
    public FormTemplate Import(string excelPath)
    {
        using var workbook = new XLWorkbook(excelPath);
        var metaSheet = workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("TemplateMeta", StringComparison.OrdinalIgnoreCase));
        if (metaSheet is null)
        {
            throw new InvalidOperationException("未找到 TemplateMeta 工作表。请在 Excel 中提供模板元数据。");
        }

        var templateId = metaSheet.Cell("B1").GetString().Trim();
        var templateName = metaSheet.Cell("B2").GetString().Trim();
        var version = metaSheet.Cell("B3").GetString().Trim();
        var background = metaSheet.Cell("B4").GetString().Trim();

        if (string.IsNullOrWhiteSpace(templateId)) templateId = Path.GetFileNameWithoutExtension(excelPath);
        if (string.IsNullOrWhiteSpace(templateName)) templateName = templateId;
        if (string.IsNullOrWhiteSpace(version)) version = "1.0.0";

        var fields = new List<FormField>();
        var row = 7; // row6 header, row7 data
        while (!metaSheet.Cell(row, 1).IsEmpty())
        {
            var fieldId = metaSheet.Cell(row, 1).GetString().Trim();
            var name = metaSheet.Cell(row, 2).GetString().Trim();
            var typeText = metaSheet.Cell(row, 3).GetString().Trim();

            if (!Enum.TryParse<FieldType>(typeText, true, out var fieldType))
            {
                throw new InvalidOperationException($"字段 {fieldId} 的类型 {typeText} 不支持。");
            }

            var x = metaSheet.Cell(row, 4).GetDouble();
            var y = metaSheet.Cell(row, 5).GetDouble();
            var width = metaSheet.Cell(row, 6).GetDouble();
            var height = metaSheet.Cell(row, 7).GetDouble();
            var placeholder = ToNullable(metaSheet.Cell(row, 8).GetString());
            var options = ToOptions(metaSheet.Cell(row, 9).GetString());
            var required = ToBool(metaSheet.Cell(row, 10).GetString());
            var min = ToNullableDouble(metaSheet.Cell(row, 11).GetString());
            var max = ToNullableDouble(metaSheet.Cell(row, 12).GetString());
            var regex = ToNullable(metaSheet.Cell(row, 13).GetString());
            var blockSubmit = ToBool(metaSheet.Cell(row, 14).GetString());

            var rule = new ValidationRule(required, min, max, regex, blockSubmit);
            fields.Add(new FormField(fieldId, name, fieldType, x, y, width, height, placeholder, options, rule));
            row++;
        }

        return new FormTemplate(templateId, version, templateName, background, fields);
    }

    private static string? ToNullable(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string[]? ToOptions(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        var tokens = trimmed.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
        return tokens.Length == 0 ? null : tokens;
    }

    private static bool ToBool(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "1" or "true" or "yes" or "y" or "是";
    }

    private static double? ToNullableDouble(string value)
    {
        return double.TryParse(value, out var result) ? result : null;
    }
}
