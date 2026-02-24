using ClosedXML.Excel;
using Client.Domain;
using Client.Infrastructure;
using Xunit;

namespace Client.Infrastructure.Tests;

public class ExcelTemplateImporterTests
{
    [Fact]
    public void Import_ShouldBuildTemplateFromTemplateMetaSheet()
    {
        var path = Path.Combine(Path.GetTempPath(), $"excel-import-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet("TemplateMeta");
                ws.Cell("A1").Value = "TemplateId";
                ws.Cell("B1").Value = "equip-check";
                ws.Cell("A2").Value = "TemplateName";
                ws.Cell("B2").Value = "设备点检";
                ws.Cell("A3").Value = "Version";
                ws.Cell("B3").Value = "2.1.0";

                ws.Cell(6, 1).Value = "FieldId";
                ws.Cell(6, 2).Value = "Name";
                ws.Cell(6, 3).Value = "Type";
                ws.Cell(6, 4).Value = "X";
                ws.Cell(6, 5).Value = "Y";
                ws.Cell(6, 6).Value = "Width";
                ws.Cell(6, 7).Value = "Height";
                ws.Cell(6, 8).Value = "Placeholder";
                ws.Cell(6, 9).Value = "Options";
                ws.Cell(6, 10).Value = "Required";
                ws.Cell(6, 11).Value = "Min";
                ws.Cell(6, 12).Value = "Max";

                ws.Cell(7, 1).Value = "temperature";
                ws.Cell(7, 2).Value = "温度";
                ws.Cell(7, 3).Value = "Number";
                ws.Cell(7, 4).Value = 40;
                ws.Cell(7, 5).Value = 120;
                ws.Cell(7, 6).Value = 100;
                ws.Cell(7, 7).Value = 30;
                ws.Cell(7, 10).Value = "1";
                ws.Cell(7, 11).Value = "0";
                ws.Cell(7, 12).Value = "80";

                wb.SaveAs(path);
            }

            var importer = new ExcelTemplateImporter();
            var template = importer.Import(path);

            Assert.Equal("equip-check", template.TemplateId);
            Assert.Equal("2.1.0", template.Version);
            Assert.Single(template.Fields);
            Assert.Equal("temperature", template.Fields[0].FieldId);
            Assert.Equal(0, template.Fields[0].Rule!.Min);
            Assert.Equal(80, template.Fields[0].Rule!.Max);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Import_ShouldParseOptionsAndFlags()
    {
        var path = Path.Combine(Path.GetTempPath(), $"excel-import-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet("TemplateMeta");
                ws.Cell("A1").Value = "TemplateId";
                ws.Cell("B1").Value = "line-check";
                ws.Cell("A2").Value = "TemplateName";
                ws.Cell("B2").Value = "产线巡检";
                ws.Cell("A3").Value = "Version";
                ws.Cell("B3").Value = "1.0.0";

                ws.Cell(6, 1).Value = "FieldId";
                ws.Cell(6, 2).Value = "Name";
                ws.Cell(6, 3).Value = "Type";
                ws.Cell(6, 4).Value = "X";
                ws.Cell(6, 5).Value = "Y";
                ws.Cell(6, 6).Value = "Width";
                ws.Cell(6, 7).Value = "Height";
                ws.Cell(6, 9).Value = "Options";
                ws.Cell(6, 10).Value = "Required";
                ws.Cell(6, 14).Value = "BlockSubmit";

                ws.Cell(7, 1).Value = "status";
                ws.Cell(7, 2).Value = "状态";
                ws.Cell(7, 3).Value = "Select";
                ws.Cell(7, 4).Value = 80;
                ws.Cell(7, 5).Value = 60;
                ws.Cell(7, 6).Value = 120;
                ws.Cell(7, 7).Value = 30;
                ws.Cell(7, 9).Value = "正常|异常|停机";
                ws.Cell(7, 10).Value = "是";
                ws.Cell(7, 14).Value = "yes";

                wb.SaveAs(path);
            }

            var importer = new ExcelTemplateImporter();
            var template = importer.Import(path);
            var field = Assert.Single(template.Fields);

            Assert.Equal(FieldType.Select, field.Type);
            Assert.Equal(new[] { "正常", "异常", "停机" }, field.Options);
            Assert.True(field.Rule!.Required);
            Assert.True(field.Rule.BlockSubmitOnError);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Import_ShouldThrow_WhenTemplateMetaSheetMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"excel-import-{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var wb = new XLWorkbook())
            {
                wb.AddWorksheet("Sheet1");
                wb.SaveAs(path);
            }

            var importer = new ExcelTemplateImporter();

            Assert.Throws<InvalidOperationException>(() => importer.Import(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
