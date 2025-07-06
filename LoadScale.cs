using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace wpf522
{
    public class Loadscale
    {
        private string exepath = ""; 
        private string scale_size;
        private string scale_unit;

        private void LoadScale()
        {
            string line;
            if (!File.Exists(exepath + "scales.csv"))
            {
                using (StreamWriter sw = new StreamWriter(exepath + "scales.csv"))
                {
                    sw.WriteLine("default,100,100,px,100,1");

                }
            }
            using (StreamReader sr = new StreamReader(exepath + "scales.csv"))
            {
                line = sr.ReadLine();
                try
                {
                    while (line != null)
                    {
                        if (line != null)
                        {
                            string[] scale_name = line.Split(',');
                            if (scale_name.Length == 6)
                            {

                                scale_size = scale_name[4];
                                scale_unit = scale_name[3];
                            }
                        }
                        line = sr.ReadLine();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void SaveCSV(string filename, ref ListView my_listview)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                foreach (ListViewItem item in my_listview.Items)
                {
                    string line = item.Content.ToString();
                    sw.WriteLine(line);
                }

            }
        }
        public void ReadCSV(string filename, ref ListView my_listview)
        {
            my_listview.Items.Clear();
            using (StreamReader sr = new StreamReader(filename))
            {
                string line = sr.ReadLine();
                while (line != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] temp_list = line.Split(',');
                        if (temp_list.Length == 5)
                        {

                        }
                        else if (line.StartsWith("image_path="))
                        {

                        }
                    }
                    line = sr.ReadLine();
                }

            }
        }

    }
}

