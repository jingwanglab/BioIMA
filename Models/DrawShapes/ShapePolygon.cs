using wpf522.Models.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace wpf522.Models.DrawShapes
{
    public class ShapePolygon : ShapeArea
    {
        /// <summary>
        /// 多点
        /// </summary>
        public ObservableCollection<Point> Points { get; set; } = new ObservableCollection<Point>();
        /// <summary>
        /// 起始点的X
        /// </summary>
        public double StartX { get; set; }
        /// <summary>
        /// 起始点的Y
        /// </summary>
        public double StartY { get; set; }

        public ShapePolygon()
        {
            ShapeType = ShapeType.Polygon;
        }

        /// <summary>
        /// 转换到box
        /// </summary>
        /// <returns></returns>
        public ShapeBox ConvertToBox()
        {
            
            var minx = Points.Select(p=>p.X).Min() + StartX;
            var miny = Points.Select(p => p.Y).Min() + StartY;
            var maxx = Points.Select(p => p.X).Max() + StartX;
            var maxy = Points.Select(p => p.Y).Max() + StartY;
            ShapeBox box = new ShapeBox() { X = minx, Y = miny, Width = maxx - minx, Height = maxy - miny, TypeName = TypeName };
            return box;
        }
    }
}
