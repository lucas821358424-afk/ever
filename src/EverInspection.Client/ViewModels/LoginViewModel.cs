using System;
using System.Windows;
using EverInspection.Client.Services;

namespace EverInspection.Client.ViewModels
{
    public sealed class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly FormSelectionViewModel _formSelection;
        private readonly HistoryViewModel _history;
        private readonly MainViewModel _main;

        private string _userId = "operator01";
        private string _password = "123456";
        private bool _isOnline = true;

        public LoginViewModel(AuthService authService, FormSelectionViewModel formSelection, HistoryViewModel history, MainViewModel main)
        {
            _authService = authService;
            _formSelection = formSelection;
            _history = history;
            _main = main;
            LoginCommand = new RelayCommand(Login);
        }

        public string UserId { get => _userId; set => SetProperty(ref _userId, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public bool IsOnline { get => _isOnline; set => SetProperty(ref _isOnline, value); }

        public RelayCommand LoginCommand { get; }

        private void Login()
        {
            try
            {
                var user = _authService.Login(UserId, Password, IsOnline);
                if (user == null)
                {
                    MessageBox.Show("账号或密码错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _main.CurrentUserDisplay = $"{user.UserName}({user.UserId})";
                _main.NetworkStatus = IsOnline ? "在线" : "离线";
                _formSelection.LoadTemplates(user.UserId);
                _history.Load(user.UserId);
                MessageBox.Show("登录成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
