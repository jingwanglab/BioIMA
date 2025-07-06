using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf522.Models
{



    public class ProjectHistoryItem : INotifyPropertyChanged
    {



        public string ProjectName { get; set; }



        public string ProjectPath { get; set; }



        public string ProjectDir { get; set; }



        public string SaveTargetDataDir { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

