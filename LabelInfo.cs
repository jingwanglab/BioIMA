using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace wpf522
{
    public class LabelInfo : INotifyPropertyChanged
    {
        private string name;
        private Color color;
        private bool isChecked;

        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }

        public Color Color
        {
            get => color;
            set { color = value; OnPropertyChanged(nameof(Color)); }
        }

        public bool IsChecked
        {
            get => isChecked;
            set { isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public float[] MaskData { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

