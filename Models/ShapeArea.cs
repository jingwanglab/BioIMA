using wpf522.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace wpf522.Models
{



    public abstract class ShapeArea : INotifyPropertyChanged
    {



        public ShapeType ShapeType { get; set; }



        public string TypeName { get; set; } = null;



        [JsonIgnore]
        public bool IsSelected { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;



        public void ChangeProperty(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


  
    }
}

