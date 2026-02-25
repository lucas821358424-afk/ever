using System;
using System.Collections.Generic;
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
        private bool _showPanelId;
        private bool _showMachineType;
        private bool _panelIdRequired;
        private bool _machineTypeRequired;
        private DateTime _inspectionDate = DateTime.Today;
        private string _inspectionTime = DateTime.Now.ToString("HH:mm:ss");
        private string _selectedEqId;
        private string _selectedMachineType;
        private string _panelId;

        public DynamicFormViewModel(FormService formService, TemplateService templateService)
        {
            _formService = formService;
            _templateService = templateService;
            Rows = new ObservableCollection<DynamicRowItem>();
            EqIdOptions = new ObservableCollection<string>();
            MachineTypeOptions = new ObservableCollection<string>();
            SaveDraftCommand = new RelayCommand(SaveDraft, () => Rows.Count > 0);
            SubmitCommand = new RelayCommand(Submit, () => Rows.Count > 0);
        }

        public ObservableCollection<DynamicRowItem> Rows { get; }
        public ObservableCollection<string> EqIdOptions { get; }
        public ObservableCollection<string> MachineTypeOptions { get; }
        public string TemplateName { get => _templateName; set => SetProperty(ref _templateName, value); }
        public string TemplateVersion { get => _templateVersion; set => SetProperty(ref _templateVersion, value); }
        public bool AllowSubmitOutOfSpec { get => _allowSubmitOutOfSpec; set => SetProperty(ref _allowSubmitOutOfSpec, value); }
        public string SaveHint { get => _saveHint; set => SetProperty(ref _saveHint, value); }
        public bool ShowPanelId { get => _showPanelId; set => SetProperty(ref _showPanelId, value); }
        public bool ShowMachineType { get => _showMachineType; set => SetProperty(ref _showMachineType, value); }
        public DateTime InspectionDate { get => _inspectionDate; set => SetProperty(ref _inspectionDate, value); }
        public string InspectionTime { get => _inspectionTime; set => SetProperty(ref _inspectionTime, value); }
        public string SelectedEqId { get => _selectedEqId; set => SetProperty(ref _selectedEqId, value); }
        public string SelectedMachineType { get => _selectedMachineType; set => SetProperty(ref _selectedMachineType, value); }
        public string PanelId { get => _panelId; set => SetProperty(ref _panelId, value); }

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
            InspectionDate = DateTime.Today;
            InspectionTime = DateTime.Now.ToString("HH:mm:ss");
            ShowPanelId = template.EnablePanelId;
            ShowMachineType = template.EnableMachineType;
            _panelIdRequired = IsHeaderRequired(template, "PanelId");
            _machineTypeRequired = IsHeaderRequired(template, "MachineType");

            EqIdOptions.Clear();
            foreach (var eq in template.EqIdOptions)
            {
                EqIdOptions.Add(eq);
            }

            MachineTypeOptions.Clear();
            foreach (var machine in template.MachineTypeOptions)
            {
                MachineTypeOptions.Add(machine);
            }

            SelectedEqId = EqIdOptions.FirstOrDefault();
            SelectedMachineType = MachineTypeOptions.FirstOrDefault();
            PanelId = string.Empty;

            Rows.Clear();

            string lastProcess = null;
            for (var i = 0; i < template.Rows.Count; i++)
            {
                var row = template.Rows[i];
                var isContinuation = !string.IsNullOrWhiteSpace(lastProcess) && string.Equals(lastProcess, row.Process, StringComparison.OrdinalIgnoreCase);

                var item = new DynamicRowItem
                {
                    RowIndex = i,
                    Process = row.Process,
                    ProcessGroup = row.ProcessGroup,
                    ProcessDisplay = isContinuation ? string.Empty : row.Process,
                    IsProcessContinuation = isContinuation,
                    CheckItem = row.ItemName,
                    Unit = row.Unit,
                    Min = row.Min,
                    Max = row.Max,
                    Required = row.Required,
                    Value = string.Empty
                };
                item.PropertyChanged += OnRowChanged;
                Rows.Add(item);
                lastProcess = row.Process;
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
            if (!ValidateHeader(out var headerError))
            {
                MessageBox.Show(headerError, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentTemplateId) || string.IsNullOrWhiteSpace(_userId))
            {
                MessageBox.Show("请先完成登录并选择表单", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var draft = _formService.CreateDraft(_currentTemplateId, TemplateVersion, _userId, BuildHeaderValues(), Rows);
            SaveHint = $"草稿已保存: {draft.FormInstanceId}";
        }

        private void Submit()
        {
            if (!ValidateHeader(out var headerError))
            {
                MessageBox.Show(headerError, "提交失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            var instance = _formService.CreateDraft(_currentTemplateId, TemplateVersion, _userId, BuildHeaderValues(), Rows);
            _formService.Submit(instance);
            SaveHint = $"提交成功: {instance.FormInstanceId}";
        }

        private bool ValidateHeader(out string message)
        {
            if (string.IsNullOrWhiteSpace(SelectedEqId))
            {
                message = "请先选择机台编号(EQ ID)";
                return false;
            }

            if (ShowMachineType && _machineTypeRequired && string.IsNullOrWhiteSpace(SelectedMachineType))
            {
                message = "当前表单要求填写机种";
                return false;
            }

            if (ShowPanelId && _panelIdRequired && string.IsNullOrWhiteSpace(PanelId))
            {
                message = "当前表单要求填写 Panel ID";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private Dictionary<string, string> BuildHeaderValues()
        {
            return new Dictionary<string, string>
            {
                ["EqId"] = SelectedEqId ?? string.Empty,
                ["MachineType"] = ShowMachineType ? (SelectedMachineType ?? string.Empty) : string.Empty,
                ["PanelId"] = ShowPanelId ? (PanelId ?? string.Empty) : string.Empty,
                ["InspectionDate"] = InspectionDate.ToString("yyyy-MM-dd"),
                ["InspectionTime"] = InspectionTime ?? string.Empty
            };
        }

        private static bool IsHeaderRequired(TemplateDefinition template, string key)
        {
            var header = template.HeaderFields.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            return header != null && header.Required;
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
