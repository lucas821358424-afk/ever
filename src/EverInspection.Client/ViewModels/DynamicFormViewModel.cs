using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using EverInspection.Client.Models;
using EverInspection.Client.Services;

namespace EverInspection.Client.ViewModels
{
    public sealed class DynamicFormViewModel : ObservableObject
    {
        private readonly FormService _formService;
        private readonly TemplateService _templateService;

        private string _templateName;
        private string _templateVersion;
        private string _currentTemplateId;
        private string _userId;
        private bool _allowSubmitOutOfSpec;
        private string _saveHint;

        public DynamicFormViewModel(FormService formService, TemplateService templateService)
        {
            _formService = formService;
            _templateService = templateService;
            Rows = new ObservableCollection<DynamicRowItem>();
            SaveDraftCommand = new RelayCommand(SaveDraft, () => Rows.Count > 0);
            SubmitCommand = new RelayCommand(Submit, () => Rows.Count > 0);
        }

        public ObservableCollection<DynamicRowItem> Rows { get; }
        public string TemplateName { get => _templateName; set => SetProperty(ref _templateName, value); }
        public string TemplateVersion { get => _templateVersion; set => SetProperty(ref _templateVersion, value); }
        public bool AllowSubmitOutOfSpec { get => _allowSubmitOutOfSpec; set => SetProperty(ref _allowSubmitOutOfSpec, value); }
        public string SaveHint { get => _saveHint; set => SetProperty(ref _saveHint, value); }

        public RelayCommand SaveDraftCommand { get; }
        public RelayCommand SubmitCommand { get; }

        public void LoadTemplate(string templateId, string userId)
        {
            var template = _templateService.GetLatestTemplates().FirstOrDefault(x => x.TemplateId == templateId);
            if (template == null)
            {
                return;
            }

            _currentTemplateId = template.TemplateId;
            _userId = userId;
            TemplateName = template.TemplateName;
            TemplateVersion = template.Version;
            Rows.Clear();

            for (var i = 0; i < template.Rows.Count; i++)
            {
                var row = template.Rows[i];
                var item = new DynamicRowItem
                {
                    RowIndex = i,
                    Process = row.Process,
                    CheckItem = row.ItemName,
                    Unit = row.Unit,
                    Min = row.Min,
                    Max = row.Max,
                    Required = row.Required,
                    Value = string.Empty
                };
                item.PropertyChanged += OnRowChanged;
                Rows.Add(item);
            }

            SaveDraftCommand.RaiseCanExecuteChanged();
            SubmitCommand.RaiseCanExecuteChanged();
        }

        private void OnRowChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DynamicRowItem.Value))
            {
                return;
            }

            var row = (DynamicRowItem)sender;
            if (string.IsNullOrWhiteSpace(row.Value))
            {
                row.IsOutOfSpec = false;
                return;
            }

            if (!decimal.TryParse(row.Value, out var value))
            {
                row.IsOutOfSpec = true;
                return;
            }

            var under = row.Min.HasValue && value < row.Min.Value;
            var over = row.Max.HasValue && value > row.Max.Value;
            row.IsOutOfSpec = under || over;
        }

        private void SaveDraft()
        {
            if (string.IsNullOrWhiteSpace(_currentTemplateId) || string.IsNullOrWhiteSpace(_userId))
            {
                MessageBox.Show("请先完成登录并选择表单", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var draft = _formService.CreateDraft(_currentTemplateId, TemplateVersion, _userId, Rows);
            SaveHint = $"草稿已保存: {draft.FormInstanceId}";
        }

        private void Submit()
        {
            if (!Rows.All(IsValid))
            {
                MessageBox.Show("存在必填项未填或非数字输入", "提交失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!AllowSubmitOutOfSpec && Rows.Any(x => x.IsOutOfSpec))
            {
                MessageBox.Show("存在超规格数据，当前配置不允许提交", "提交失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var instance = _formService.CreateDraft(_currentTemplateId, TemplateVersion, _userId, Rows);
            _formService.Submit(instance);
            SaveHint = $"提交成功: {instance.FormInstanceId}";
        }

        private static bool IsValid(DynamicRowItem row)
        {
            if (row.Required && string.IsNullOrWhiteSpace(row.Value))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(row.Value) && !decimal.TryParse(row.Value, out _))
            {
                return false;
            }

            return true;
        }
    }
}
