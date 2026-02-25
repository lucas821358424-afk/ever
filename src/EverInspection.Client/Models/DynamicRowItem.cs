using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverInspection.Client.Models
{
    public sealed class DynamicRowItem : INotifyPropertyChanged
    {
        private string _value;
        private bool _isOutOfSpec;

        public int RowIndex { get; set; }
        public string Process { get; set; }
        public string ProcessGroup { get; set; }
        public string ProcessDisplay { get; set; }
        public bool IsProcessContinuation { get; set; }
        public string CheckItem { get; set; }
        public string Unit { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public bool Required { get; set; }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public bool IsOutOfSpec
        {
            get => _isOutOfSpec;
            set
            {
                _isOutOfSpec = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
