using wpf522.CustomDialogs;
using wpf522.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace wpf522.Expends
{
    public static class ObjectExpend
    {

        /// <summary>
        /// 深度拷贝
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="v"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T CopyTo<V, T>(this V v, T t)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            var vPros = typeof(V).GetProperties();
            foreach (var item in properties)
            {
                var vP = vPros.Where(p => p.Name.Equals(item.Name)).FirstOrDefault();
                if (vP != null && item.CanWrite)
                {
                    item.SetValue(t, vP.GetValue(v));
                }
            }
            return t;
        }
    
        /// <summary>
        /// 检测路径是否都存在，如果不存在则创建
        /// </summary>
        /// <param name="path"></param>
        public static string CheckPath(this string path)
        {
            var dir = "";
            if (path.Contains("."))
            {
                dir = Directory.GetDirectoryRoot(path);
            }
            else
            {
                dir = path;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return path;
        }
        /// <summary>
        /// 打开文件夹对话框
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static async Task<string> OpenFolderDialogAsync(this MetroWindow instance)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog(MainWindow.Instance, DirectoryItem.Dir);
            await instance.ShowMetroDialogAsync(folderBrowserDialog);
            await folderBrowserDialog.WaitForResult();
            try
            {
                await instance.HideMetroDialogAsync(folderBrowserDialog);
            }
            catch (Exception)
            {
            }
            if (folderBrowserDialog.OptionResult == true)
            {
                return folderBrowserDialog.SelectedDirectoryPath;
            }
            return null;
        }


        /// <summary>
        /// 打开类型转换对话框
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static async Task<bool> OpenConverterTypesDialogAsync(this MetroWindow instance, MainModel model)
        {
            GlobalTypeConverter convertDialog = new GlobalTypeConverter(MainWindow.Instance, model);
            await instance.ShowMetroDialogAsync(convertDialog);
            await convertDialog.WaitForResult();
            try
            {
                await instance.HideMetroDialogAsync(convertDialog);
            }
            catch (Exception)
            {
            }
            if (convertDialog.IsSure == true)
            {
                if (convertDialog.SelectedFrom == null || convertDialog.SelectedTarget == null) return false;
                string fromName = convertDialog.SelectedFrom.TypeName;
                string targetName = convertDialog.SelectedTarget.TypeName;
                try
                {
                    int modifys = 0;
                    foreach (var item in model.ImageModels)
                    {
                        foreach (var s in item.Shapes)
                        {
                            if (s.TypeName.Equals(fromName))
                            {
                                s.TypeName = targetName;
                                modifys++;
                            }
                        }
                    }
                    await MainWindow.Instance.ShowMessageAsync("修改", String.Format("一共修改 : {0}个", modifys));
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("异常!", ex.ToString());
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 是否是绝对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsAbsolutePath(this string path)
        {
            return path.Contains(":");
        }
    }
}
