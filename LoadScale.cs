using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace wpf522
{
    public class Loadscale
    {
        private string exepath = ""; // define and initialize exepath
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
                    // Add item to your WPF ListView
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
                                // Add item to your WPF ListView
                                scale_size = scale_name[4];
                                scale_unit = scale_name[3];
                            }
                        }
                        line = sr.ReadLine();
                    }
                    // Select the last item in your WPF ListView
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
                // Write additional data to the CSV file
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
                            // Add item to your WPF ListView
                        }
                        else if (line.StartsWith("image_path="))
                        {
                            // Handle image path
                        }
                    }
                    line = sr.ReadLine();
                }
                // Select the last item in your WPF ListView
            }
        }
        // Other methods and variables as per your implementation
    }
}
