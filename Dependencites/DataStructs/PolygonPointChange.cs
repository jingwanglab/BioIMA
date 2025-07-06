using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace wpf522.Dependencites.DataStructs
{
    public class PolygonPointChange
    {



        public Polygon Polygon { get; set; }



        public int Index { get; set; }



        public Point OldValue { get; set; }



        public Point NewValue { get; set; }
    }
}

