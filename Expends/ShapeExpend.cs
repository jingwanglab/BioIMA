using wpf522.Models.DrawShapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace wpf522.Expends
{
    public static class ShapeExpend
    {





        public static void ShapeBindingRectangle(this ShapeBox box, System.Windows.Shapes.Rectangle rectangle)
        {
            box.BindingEx("Width", rectangle, Rectangle.WidthProperty);
            box.BindingEx("Height", rectangle, Rectangle.HeightProperty);

            box.BindingEx("X", rectangle, Canvas.LeftProperty);
            box.BindingEx("Y", rectangle, Canvas.TopProperty);
        }


 
    }
}

