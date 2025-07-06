using System.ComponentModel;
using System.Windows.Ink;

namespace agmeasure
{
    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string meaInfo;
        public string MeaInfo
        {
            get => meaInfo;
            set
            {
                meaInfo = value;
                OnPropertyChanged("MeaInfo");
            }
        }

        private StrokeCollection inkStrokes;
        public StrokeCollection InkStrokes
        {
            get { return inkStrokes; }
            set
            {
                inkStrokes = value;
                OnPropertyChanged("InkStrokes");
            }
        }

        public class ViewModel2 : INotifyPropertyChanged
        {
            private string meaInfo;
            private bool isAngleMeasureMode;
            public event PropertyChangedEventHandler PropertyChanged;

            public string MeaInfo
            {
                get => meaInfo;
                set
                {
                    if (meaInfo != value)
                    {
                        meaInfo = value;
                        OnPropertyChanged(nameof(MeaInfo));
                    }
                }
            }

            public bool IsAngleMeasureMode
            {
                get => isAngleMeasureMode;
                set
                {
                    if (isAngleMeasureMode != value)
                    {
                        isAngleMeasureMode = value;
                        OnPropertyChanged(nameof(IsAngleMeasureMode));
                    }
                }
            }

            public StrokeCollection InkStrokes { get; set; }

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}

