using Xunit;
using Client.Domain;
using Client.Infrastructure;

namespace Client.Infrastructure.Tests;

public class SqliteStoreTests
{
    [Fact]
    public void SaveAndLoadTemplate_ShouldRoundTrip()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ever-test-{Guid.NewGuid():N}.db");
        try
        {
            var store = new SqliteStore(dbPath);
            store.Initialize();

            var template = new FormTemplate(
                "demo",
                "1.0.1",
                "示例模板",
                "background.png",
                new List<FormField>
                {
                    new("operator", "填写人", FieldType.Text, 10, 10, 100, 30, null, null, new ValidationRule(Required: true))
                });

            store.SaveTemplate(template);
            var loaded = store.LoadTemplate("demo");

            Assert.NotNull(loaded);
            Assert.Equal("demo", loaded!.TemplateId);
            Assert.Equal("1.0.1", loaded.Version);
            Assert.Single(loaded.Fields);
            Assert.Equal("operator", loaded.Fields[0].FieldId);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void LoadTemplate_ShouldReturnNull_WhenTemplateNotExists()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ever-test-{Guid.NewGuid():N}.db");
        try
        {
            var store = new SqliteStore(dbPath);
            store.Initialize();

            var loaded = store.LoadTemplate("missing-template");

            Assert.Null(loaded);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

}
