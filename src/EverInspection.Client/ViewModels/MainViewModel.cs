using EverInspection.Client.Services;

namespace EverInspection.Client.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private string _networkStatus = "在线";
        private string _currentUserDisplay = "未登录";

        public string NetworkStatus
        {
            get => _networkStatus;
            set => SetProperty(ref _networkStatus, value);
        }

        public string CurrentUserDisplay
        {
            get => _currentUserDisplay;
            set => SetProperty(ref _currentUserDisplay, value);
        }

        public LoginViewModel Login { get; private set; }
        public FormSelectionViewModel FormSelection { get; private set; }
        public DynamicFormViewModel DynamicForm { get; private set; }
        public HistoryViewModel History { get; private set; }

        public static MainViewModel Create()
        {
            var db = new LocalDbService();
            var auth = new AuthService(db);
            var template = new TemplateService(db);
            var permission = new PermissionService(db);
            var form = new FormService(db);
            var sync = new SyncService(form, template, db);

            var main = new MainViewModel();
            main.DynamicForm = new DynamicFormViewModel(form, template);
            main.FormSelection = new FormSelectionViewModel(template, form, permission, main.DynamicForm);
            main.History = new HistoryViewModel(form);
            main.Login = new LoginViewModel(auth, main.FormSelection, main.History, main);
            main.FormSelection.LoadTemplates(string.Empty);
            return main;
        }
    }
}
