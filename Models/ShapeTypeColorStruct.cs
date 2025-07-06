using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace wpf522.Models
{



    public class ShapeTypeColorStruct : INotifyPropertyChanged
    {



        private string color;

        private string typeName;

        private bool isChecked;



        public string TypeName
        {
            get { return typeName; }
            set
            {
                typeName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TypeName"));
            }
        }



        public string Color
        {
            get { return color; }
            set
            {
                color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TypeName"));
                if (MainWindow.Instance is not null && MainWindow.Instance.MainModel is not null &&
                    MainWindow.Instance.MainModel.CurrentImageModel is not null)
                { 

                    MainWindow.Instance.MainModel.CurrentImageModel.ChangeShapeProperty("TypeName", TypeName);
                }
            }
        }



        [JsonIgnore]
        public bool IsChecked

        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

