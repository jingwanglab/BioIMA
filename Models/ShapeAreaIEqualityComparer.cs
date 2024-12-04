using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace wpf522.Models
{
    internal class ShapeAreaIEqualityComparer : IEqualityComparer<ShapeArea>
    {
        public bool Equals(ShapeArea? x, ShapeArea? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.ShapeType == y.ShapeType && x.TypeName.Equals(y.TypeName);
        }

        public int GetHashCode([DisallowNull] ShapeArea obj)
        {
            return obj.GetHashCode();
        }
    }
}
