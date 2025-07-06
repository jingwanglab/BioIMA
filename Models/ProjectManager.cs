using wpf522.CustomCommand;
using wpf522.Expends;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace wpf522.Models
{



    public class ProjectManager : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private string selectedFolderPath;
        public string SelectedFolderPath
        {
            get { return selectedFolderPath; }
            set
            {
                selectedFolderPath = value;
                OnPropertyChanged(nameof(SelectedFolderPath));
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



        [JsonIgnore]
        public static ProjectManager Instance = null;



        public static string ProjectManagerConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project.json");



        public static string ProjectConfigDefaultFileExName = ".ai";



        public ObservableCollection<ProjectHistoryItem> ProjectHistory { get; set; } = new ObservableCollection<ProjectHistoryItem>();



        [JsonIgnore]
        public ObservableCollection<ProjectHistoryItem> ProjectHistoryView { get; set; } = new ObservableCollection<ProjectHistoryItem>();



        [JsonIgnore]
        public ICommand SearchProjectCommand { get; set; }



        [JsonIgnore]
        public ICommand OpenProjectCommand { get; set; }



        [JsonIgnore]
        public ICommand CreateProjectCommand { get; set; }



        [JsonIgnore]
        public ICommand PreviousStepCommand { get; set; }



        [JsonIgnore]
        public ICommand SureCreateProjectCommand { get; set; }



        [JsonIgnore]
        public ICommand OpenDirCommand { get; set; }



        [JsonIgnore]
        public ICommand CloseWindowCommand { get; set; }



        [JsonIgnore]
        public ICommand OpenOtherProjectFileCommand { get; set; }



        [JsonIgnore]
        public ICommand OpenOtherProjectFolderCommand { get; set; }



        [JsonIgnore]
        public ProjectHistoryItem SelectedItem { get; set; }



        [JsonIgnore]
        public ProjectHistoryItem CreateProjectItem { get; set; } = new ProjectHistoryItem();



        [JsonIgnore]
        public MainModel MainModel { get; set; }



        [JsonIgnore]
        public int PageIndex { get; set; } = 0;



        [JsonIgnore]
        public string InputName { get; set; }

   
        public ProjectManager()
        {
            SearchProjectCommand = new SampleCommand(o => true, o => {
                try
                {
                    ProjectHistoryView.Clear();
                    if (InputName == null || InputName.Equals(""))
                    {
                        foreach (var item in ProjectHistory)
                        {
                            ProjectHistoryView.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in ProjectHistory.Where(p => p.ProjectName.Equals(InputName)))
                        {
                            ProjectHistoryView.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            });

            OpenProjectCommand = new SampleCommand(o => true, async o => {
                try
                {
                    if (SelectedItem == null)
                    {
                        return;
                    }
                    if (!File.Exists(SelectedItem.ProjectPath))
                    {
                        var res = await ProjectOptionWindow.Instance.ShowMessageAsync("错误", "该路径下项目已经不存在，确认后将自动删除这条记录..");
                        ProjectHistory.Remove(SelectedItem);
                        if (ProjectHistoryView.Contains(SelectedItem))
                            ProjectHistoryView.Remove(SelectedItem);
                    }
                    MainModel = new MainModel(SelectedItem);
                    await SaveProjectManager();
                    ProjectOptionWindow.Instance.Close();
                }
                catch (Exception ex)
                {
                    await ProjectOptionWindow.Instance.ShowMessageAsync("打开异常!", ex.ToString());
                }
            });

            CreateProjectCommand = new SampleCommand(o => true, async o => {
                try
                {
                    PageIndex = 1;
                }
                catch (Exception ex)
                {
                    await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", ex.ToString());
                }
            });

            PreviousStepCommand = new SampleCommand(o => true, o => {
                PageIndex = 0;
            });

            SureCreateProjectCommand = new SampleCommand(o => CreateProjectItem.ProjectName != null, async o => {
                try
                {
                    if (!Directory.Exists(CreateProjectItem.ProjectDir))
                    {
                        await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", "项目目录不存在");
                        return;
                    }
                    if (!Directory.Exists(CreateProjectItem.SaveTargetDataDir))
                    {
                        await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", "保存目录不存在");
                        return;
                    }
                    CreateProjectItem.ProjectPath = System.IO.Path.Combine(CreateProjectItem.ProjectDir, CreateProjectItem.ProjectName + ProjectConfigDefaultFileExName);
                    MainModel = new MainModel(CreateProjectItem);
                    ProjectOptionWindow.Instance.Close();
                }
                catch (Exception ex)
                {
                    await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", ex.Message);
                }
            });

            OpenDirCommand = new SampleCommand(o => true, async o => {
                try
                {
                    var dir = await ProjectOptionWindow.Instance.OpenFolderDialogAsync();
                    if (dir == null) return;
                    if (o.Equals("0"))
                    {
                        CreateProjectItem.ProjectDir = dir;
                    }
                    else
                    {
                        CreateProjectItem.SaveTargetDataDir = dir;
                    }
                }
                catch (Exception ex)
                {
                    await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", ex.ToString());
                }
            });

            CloseWindowCommand = new SampleCommand(o => true, o => {
                try
                {
                    ProjectOptionWindow.Instance.Close();
                }
                catch (Exception)
                {

                }
            });

            OpenOtherProjectFileCommand = new SampleCommand(o => true, async o => {
                try
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.Filter = "项目文件|*.ai";
                    if(open.ShowDialog() == true)
                    {
                        MainModel = new MainModel(open.FileName);
                        ProjectHistoryItem item = new ProjectHistoryItem()
                        {
                            ProjectDir = MainModel.ToolConfig.OpenUriHistory,
                            ProjectName = MainModel.ToolConfig.ProjectName,
                            ProjectPath = open.FileName,
                            SaveTargetDataDir = MainModel.ToolConfig.SaveFileConfigHistory
                        };
                        this.ProjectHistory.Add(item);
                        await SaveProjectManager();
                        ProjectOptionWindow.Instance.Close();
                    }
                }
                catch (Exception ex)
                {
                    await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", ex.ToString());
                }
            });

            OpenOtherProjectFolderCommand = new SampleCommand(o => true, async o => {
                try
                {
                    var dir = await ProjectOptionWindow.Instance.OpenFolderDialogAsync();
                    if (dir != null)
                    {
                        MainModel = MainModel.CreateMainModelByFolder(dir);
                        ProjectHistoryItem item = new ProjectHistoryItem()
                        {
                            ProjectDir = MainModel.ToolConfig.OpenUriHistory,
                            ProjectName = MainModel.ToolConfig.ProjectName,
                            ProjectPath = MainModel.ConfigSavePath,
                            SaveTargetDataDir = MainModel.ToolConfig.SaveFileConfigHistory
                        };
                        this.ProjectHistory.Add(item);
                        await SaveProjectManager();
                        ProjectOptionWindow.Instance.Close();
                    }
                }
                catch (Exception ex)
                {
                    await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", ex.ToString());
                }
            });

            SearchProjectCommand.Execute(null);
        }



        public static void LoadProjectManager()
        {
            try
            {
                if (File.Exists(ProjectManagerConfigPath))
                {
                    Instance = JsonConvert.DeserializeObject<ProjectManager>(File.ReadAllText(ProjectManagerConfigPath, Encoding.UTF8));
                    Instance.SearchProjectCommand.Execute(null);
                }
                else
                {
                    Instance = new ProjectManager();
                }
            }
            catch (Exception ex)
            {
                Instance = new ProjectManager();
            }
        }



        public static async Task SaveProjectManager()
        {
            try
            {
                var str = JsonConvert.SerializeObject(Instance);
                File.WriteAllText(ProjectManagerConfigPath, str, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                await ProjectOptionWindow.Instance.ShowMessageAsync("错误!", ex.ToString());
            }
        }
    }
}

