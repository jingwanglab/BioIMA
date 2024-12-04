using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf522.Models.Enums
{

    public enum EnumMeasureTools
    {
        [Description("测量距离")]
        distance = 0,
        [Description("测量角度")]
        angle,
    }
}