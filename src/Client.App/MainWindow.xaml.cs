using Client.Domain;
using Client.Infrastructure;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.App;

public partial class MainWindow : Window
{
    private readonly SqliteStore _store;
    private readonly ExcelTemplateImporter _excelImporter;
    private FormTemplate? _template;
    private readonly Dictionary<string, Control> _inputControls = new();

    public MainWindow()
    {
        InitializeComponent();

        var dbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "client.db");
        _store = new SqliteStore(dbPath);
        _store.Initialize();
        _excelImporter = new ExcelTemplateImporter();

        StatusText.Text = $"数据库: {dbPath}";
    }

    private void ImportExcelTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "选择模板Excel文件"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _template = _excelImporter.Import(dialog.FileName);
            _store.SaveTemplate(_template);
            RenderTemplate(_template);
            StatusText.Text = $"已导入Excel模板 {_template.Name} v{_template.Version}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败: {ex.Message}\n\n请确认 Excel 含 TemplateMeta 工作表与规范字段。", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTemplate_Click(object sender, RoutedEventArgs e)
    {
        _template = BuildSampleTemplate();
        _store.SaveTemplate(_template);
        RenderTemplate(_template);
        StatusText.Text = $"已加载模板 {_template.Name} v{_template.Version}";
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        if (_template is null)
        {
            MessageBox.Show("请先加载模板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var errors = new List<string>();
        foreach (var field in _template.Fields)
        {
            if (!_inputControls.TryGetValue(field.FieldId, out var ctrl))
            {
                continue;
            }

            string? value = ctrl switch
            {
                TextBox tb => tb.Text,
                DatePicker dp => dp.SelectedDate?.ToString("yyyy-MM-dd"),
                ComboBox cb => cb.SelectedItem?.ToString(),
                CheckBox ckb => ckb.IsChecked == true ? "true" : "false",
                _ => null
            };

            var result = FieldValidator.Validate(field, value);
            ctrl.Background = result.IsValid ? Brushes.White : Brushes.MistyRose;

            if (!result.IsValid && result.Message is not null)
            {
                errors.Add(result.Message);
            }
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "校验报警", MessageBoxButton.OK, MessageBoxImage.Warning);
            StatusText.Text = $"校验失败: {errors.Count} 项";
            return;
        }

        StatusText.Text = "校验通过，可提交";
    }

    private void RenderTemplate(FormTemplate template)
    {
        FormCanvas.Children.Clear();
        _inputControls.Clear();

        foreach (var field in template.Fields)
        {
            var element = CreateElement(field);
            if (element is null)
            {
                continue;
            }

            Canvas.SetLeft(element, field.X);
            Canvas.SetTop(element, field.Y);
            FormCanvas.Children.Add(element);

            if (element is Control c)
            {
                _inputControls[field.FieldId] = c;
            }
        }
    }

    private FrameworkElement? CreateElement(FormField field)
    {
        switch (field.Type)
        {
            case FieldType.Text:
            case FieldType.Number:
                return new TextBox { Width = field.Width, Height = field.Height, ToolTip = field.Placeholder };
            case FieldType.Date:
                return new DatePicker { Width = field.Width, Height = field.Height };
            case FieldType.Select:
                return new ComboBox
                {
                    Width = field.Width,
                    Height = field.Height,
                    ItemsSource = field.Options ?? Array.Empty<string>()
                };
            case FieldType.Checkbox:
                return new CheckBox { Width = field.Width, Height = field.Height, Content = field.Name };
            case FieldType.Label:
                return new TextBlock { Width = field.Width, Height = field.Height, Text = field.Name, FontWeight = FontWeights.Bold };
            default:
                return null;
        }
    }

    private static FormTemplate BuildSampleTemplate()
    {
        var fields = new List<FormField>
        {
            new("label_title", "设备点检表", FieldType.Label, 40, 20, 300, 30, null, null, null),
            new("operator", "填写人", FieldType.Text, 40, 80, 220, 30, "请输入姓名", null, new ValidationRule(Required: true)),
            new("check_date", "点检日期", FieldType.Date, 280, 80, 160, 30, null, null, new ValidationRule(Required: true)),
            new("temperature", "温度", FieldType.Number, 40, 130, 120, 30, "单位℃", null, new ValidationRule(Required: true, Min: 0, Max: 80, BlockSubmitOnError: true)),
            new("shift", "班次", FieldType.Select, 180, 130, 120, 30, null, new[] { "白班", "夜班" }, new ValidationRule(Required: true)),
            new("safe", "安全确认", FieldType.Checkbox, 40, 180, 150, 30, null, null, new ValidationRule(Required: true))
        };

        return new FormTemplate("demo-checklist", "1.0.0", "示例点检模板", "", fields);
    }
}
