using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf522.Method
{
    public static class ComMethod
    {





        public static int Sum(this int[] data)
        {
            int result = 0;
            for (int i = 0; i < data.Length; i++)
            {
                result += data[i];
            }
            return result;
        }





        public static double Mean(this int[] data)
        {
            double result = data.Sum();
            return result / data.Length;
        }
    }
}
