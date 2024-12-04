//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using wpf522.Models;
//using wpf522.CustomCommand;
//using static wpf522.CustomControls.CanvasOption;

//namespace wpf522.Models
//{
//    public class PointViewModel : INotifyPropertyChanged
//    {
//        private ObservableCollection<PointData> pointDataCollection;
//        public ObservableCollection<PointData> PointDataCollection
//        {
//            get => pointDataCollection;
//            set
//            {
//                pointDataCollection = value;
//                OnPropertyChanged();
//            }
//        }

//        public SampleCommand SetDrawPointCommand { get; }

//        public PointViewModel()
//        {
//            PointDataCollection = new ObservableCollection<PointData>();
//            SetDrawPointCommand = new SampleCommand(_ => true, _ => SetDrawPoint());
//        }

//        private void SetDrawPoint()
//        {
//            MessageBox.Show("SetDrawPoint Command Executed");
//            Implement your logic here
//        }

//        public event PropertyChangedEventHandler? PropertyChanged;

//        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }
//}