namespace Client.Domain;

public enum FieldType
{
    Text,
    Number,
    Date,
    Select,
    Checkbox,
    Label,
    Signature
}

public sealed record ValidationRule(
    bool Required = false,
    double? Min = null,
    double? Max = null,
    string? Regex = null,
    bool BlockSubmitOnError = false);

public sealed record FormField(
    string FieldId,
    string Name,
    FieldType Type,
    double X,
    double Y,
    double Width,
    double Height,
    string? Placeholder,
    string[]? Options,
    ValidationRule? Rule);

public sealed record FormTemplate(
    string TemplateId,
    string Version,
    string Name,
    string BackgroundImage,
    IReadOnlyList<FormField> Fields);

public sealed record ValidationResult(bool IsValid, string? Message);

public static class FieldValidator
{
    public static ValidationResult Validate(FormField field, string? value)
    {
        var rule = field.Rule;
        if (rule is null)
        {
            return new ValidationResult(true, null);
        }

        if (rule.Required && string.IsNullOrWhiteSpace(value))
        {
            return new ValidationResult(false, $"{field.Name} 为必填项");
        }

        if (field.Type == FieldType.Number && !string.IsNullOrWhiteSpace(value))
        {
            if (!double.TryParse(value, out var number))
            {
                return new ValidationResult(false, $"{field.Name} 必须是数值");
            }

            if (rule.Min.HasValue && number < rule.Min.Value)
            {
                return new ValidationResult(false, $"{field.Name} 不能小于 {rule.Min.Value}");
            }

            if (rule.Max.HasValue && number > rule.Max.Value)
            {
                return new ValidationResult(false, $"{field.Name} 不能大于 {rule.Max.Value}");
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.Regex) && !string.IsNullOrWhiteSpace(value))
        {
            var ok = System.Text.RegularExpressions.Regex.IsMatch(value, rule.Regex);
            if (!ok)
            {
                return new ValidationResult(false, $"{field.Name} 格式错误");
            }
        }

        return new ValidationResult(true, null);
    }
}
