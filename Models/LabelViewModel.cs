using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using YourNamespace.Models;

namespace YourNamespace.ViewModels
{
    public class LabelViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LabelItem> _labels;
        private LabelItem _selectedLabel;

        public ObservableCollection<LabelItem> Labels
        {
            get => _labels;
            set { _labels = value; OnPropertyChanged(); }
        }
        public LabelItem SelectedLabel { get; set; }

        //public LabelItem SelectedLabel
        //{
        //    get => _selectedLabel;
        //    set { _selectedLabel = value; OnPropertyChanged(); }
        //}

        public LabelViewModel()
        {
            Labels = new ObservableCollection<LabelItem>();
        }

        public void AddLabel(string name, Color color, float[] mask)
        {
            Labels.Add(new LabelItem { Name = name, Color = color, MaskData = mask, IsChecked = true });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
