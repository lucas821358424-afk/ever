using Xunit;
using Client.Domain;

namespace Client.Domain.Tests;

public class FieldValidatorTests
{
    [Fact]
    public void Validate_ShouldFail_WhenRequiredFieldIsEmpty()
    {
        var field = new FormField("f1", "温度", FieldType.Number, 0, 0, 10, 10, null, null, new ValidationRule(Required: true));

        var result = FieldValidator.Validate(field, "");

        Assert.False(result.IsValid);
        Assert.Contains("必填", result.Message);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumberOutOfRange()
    {
        var field = new FormField("f2", "温度", FieldType.Number, 0, 0, 10, 10, null, null, new ValidationRule(Min: 0, Max: 80));

        var result = FieldValidator.Validate(field, "100");

        Assert.False(result.IsValid);
        Assert.Contains("不能大于", result.Message);
    }

    [Fact]
    public void Validate_ShouldPass_WhenTextMatchesRegex()
    {
        var field = new FormField("f3", "工号", FieldType.Text, 0, 0, 10, 10, null, null, new ValidationRule(Regex: "^[A-Z]{2}\\d{4}$"));

        var result = FieldValidator.Validate(field, "AB1234");

        Assert.True(result.IsValid);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumberCannotParse()
    {
        var field = new FormField("f4", "电流", FieldType.Number, 0, 0, 10, 10, null, null, new ValidationRule());

        var result = FieldValidator.Validate(field, "abc");

        Assert.False(result.IsValid);
        Assert.Contains("数值", result.Message);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRegexDoesNotMatch()
    {
        var field = new FormField("f5", "工号", FieldType.Text, 0, 0, 10, 10, null, null, new ValidationRule(Regex: "^[A-Z]{2}\\d{4}$"));

        var result = FieldValidator.Validate(field, "ab1234");

        Assert.False(result.IsValid);
        Assert.Contains("格式", result.Message);
    }
}
