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
        /// <summary>
        /// 多边形
        /// </summary>
        public Polygon Polygon { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 旧的值
        /// </summary>
        public Point OldValue { get; set; }
        /// <summary>
        /// 新的值
        /// </summary>
        public Point NewValue { get; set; }
    }
}
