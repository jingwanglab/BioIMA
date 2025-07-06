using wpf522.Models.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace wpf522.Models.DrawShapes
{



    public class ShapeBox : ShapeArea
    {



        public double X { get; set; }



        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }


        public ShapeBox()
        {
            ShapeType = ShapeType.Box;
        }
    }
}

