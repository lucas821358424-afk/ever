using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using EverInspection.Client.Models;
using EverInspection.Client.Services;

namespace EverInspection.Client.ViewModels
{
    public sealed class FormSelectionViewModel : ObservableObject
    {
        private readonly TemplateService _templateService;
        private readonly FormService _formService;
        private readonly PermissionService _permissionService;
        private readonly DynamicFormViewModel _dynamicForm;

        private string _keyword;
        private string _currentUserId;
        private TemplateListItem _selected;

        public FormSelectionViewModel(TemplateService templateService, FormService formService, PermissionService permissionService, DynamicFormViewModel dynamicForm)
        {
            _templateService = templateService;
            _formService = formService;
            _permissionService = permissionService;
            _dynamicForm = dynamicForm;
            Templates = new ObservableCollection<TemplateListItem>();
            SearchCommand = new RelayCommand(Search);
            NewFormCommand = new RelayCommand(OpenNewForm, () => Selected != null);
        }

        public ObservableCollection<TemplateListItem> Templates { get; }
        public string Keyword { get => _keyword; set => SetProperty(ref _keyword, value); }

        public TemplateListItem Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    NewFormCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand SearchCommand { get; }
        public RelayCommand NewFormCommand { get; }

        public void LoadTemplates(string userId)
        {
            _currentUserId = userId;
            var templates = _templateService.GetLatestTemplates();
            var permissions = string.IsNullOrWhiteSpace(userId)
                ? new Dictionary<string, PermissionInfo>()
                : _permissionService.GetPermissionsByUser(userId);
            Bind(templates, permissions);
        }

        private void Search()
        {
            var templates = _templateService.GetLatestTemplates();
            var permissions = string.IsNullOrWhiteSpace(_currentUserId)
                ? new Dictionary<string, PermissionInfo>()
                : _permissionService.GetPermissionsByUser(_currentUserId);

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                templates = templates.Where(x => x.TemplateName.Contains(Keyword)).ToList();
            }

            Bind(templates, permissions);
        }

        private void Bind(IList<TemplateDefinition> templates, IDictionary<string, PermissionInfo> permissions)
        {
            Templates.Clear();
            foreach (var template in templates)
            {
                var visible = permissions.Count == 0 || (permissions.TryGetValue(template.TemplateId, out var p) && p.FormVisible);
                if (!visible)
                {
                    continue;
                }

                Templates.Add(new TemplateListItem
                {
                    TemplateId = template.TemplateId,
                    TemplateName = template.TemplateName,
                    Version = template.Version,
                    UpdatedAt = template.UpdatedAt.ToString("yyyy-MM-dd HH:mm"),
                    PendingUploadCount = _formService.CountPendingUpload(template.TemplateId)
                });
            }
        }

        private void OpenNewForm()
        {
            if (Selected == null)
            {
                return;
            }

            _dynamicForm.LoadTemplate(Selected.TemplateId, _currentUserId);
            MessageBox.Show($"已加载 {Selected.TemplateName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public sealed class TemplateListItem
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Version { get; set; }
        public string UpdatedAt { get; set; }
        public int PendingUploadCount { get; set; }
    }
}
