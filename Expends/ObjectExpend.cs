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
                    await MainWindow.Instance.ShowMessageAsync("�޸�", String.Format("һ���޸� : {0}��", modifys));
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("�쳣!", ex.ToString());
                }
                return true;
            }
            return false;
        }





        public static bool IsAbsolutePath(this string path)
        {
            return path.Contains(":");
        }
    }
}

