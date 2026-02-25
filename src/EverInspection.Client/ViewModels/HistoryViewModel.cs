using System;
using System.Collections.ObjectModel;
using System.Linq;
using EverInspection.Client.Models;
using EverInspection.Client.Services;

namespace EverInspection.Client.ViewModels
{
    public sealed class HistoryViewModel : ObservableObject
    {
        private readonly FormService _formService;
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        private DateTime _endDate = DateTime.Today;
        private string _templateId;
        private string _userId;

        public HistoryViewModel(FormService formService)
        {
            _formService = formService;
            Records = new ObservableCollection<FormInstance>();
            QueryCommand = new RelayCommand(Query);
        }

        public ObservableCollection<FormInstance> Records { get; }
        public DateTime StartDate { get => _startDate; set => SetProperty(ref _startDate, value); }
        public DateTime EndDate { get => _endDate; set => SetProperty(ref _endDate, value); }
        public string TemplateId { get => _templateId; set => SetProperty(ref _templateId, value); }

        public RelayCommand QueryCommand { get; }

        public void Load(string userId)
        {
            _userId = userId;
            Query();
        }

        private void Query()
        {
            Records.Clear();
            if (string.IsNullOrWhiteSpace(_userId))
            {
                return;
            }

            var items = _formService.GetInstances(_userId, TemplateId)
                .Where(x => x.CreatedAt >= StartDate && x.CreatedAt <= EndDate.AddDays(1).AddSeconds(-1));

            foreach (var item in items)
            {
                Records.Add(item);
            }
        }
    }
}
