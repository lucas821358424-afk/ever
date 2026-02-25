using System.Windows;
using EverInspection.Client.ViewModels;

namespace EverInspection.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = MainViewModel.Create();
        }
    }
}
