using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace wpf522.Expends
{
    public static class IEnumableExpend
    {
        /// <summary>
        /// 追加集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="others"></param>
        public static void AppendRange<T>(this ICollection<T> ts, IEnumerable<T> others)
        {
            foreach (var item in others)
            {
                ts.Add(item);
            }
        }

        /// <summary>
        /// 从集合中查找指定类型的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="vs"></param>
        /// <returns></returns>
        public static T FindTargetType<T, V>(this IEnumerable<V> vs) where T : V
        {
            foreach (var item in vs)
            {
                if(item is T)
                {
                    return (T)item;
                }
            }
            return default(T);
        }
        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="t"></param>
        public static void Foreach<T>(this IEnumerable<T> values, Func<T, bool> t)
        {
            foreach (var item in values)
            {
                if (t?.Invoke(item) == false)
                    break;
            }
        }

        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="t"></param>
        public static void Foreach<T>(this IEnumerable<T> values, Action<T> t)
        {
            foreach (var item in values)
            {
                t?.Invoke(item);
            }
        }
        /// <summary>
        /// 去重
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="cmp"></param>
        /// <returns></returns>
        public static IEnumerable<T> Distinct<T, IKey>(this IEnumerable<T> values, Func<T, IKey> cmp)
        {
            HashSet<IKey> keys = new HashSet<IKey>();
            List<T> target = new List<T>();
            foreach (var item in values)
            {
                var key = cmp.Invoke(item);
                if (keys.Contains(key))
                {
                    continue;
                }
                target.Add(item);
                keys.Add(key);
            }

            return target;
        }
    }
}
