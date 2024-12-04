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
    /// <summary>
    /// 项目管理
    /// </summary>
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

        /// <summary>
        /// 项目管理单例
        /// </summary>
        [JsonIgnore]
        public static ProjectManager Instance = null;
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public static string ProjectManagerConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project.json");
        /// <summary>
        /// 项目配置文件默认名称
        /// </summary>
        public static string ProjectConfigDefaultFileExName = ".ai";
        /// <summary>
        /// 项目历史
        /// </summary>
        public ObservableCollection<ProjectHistoryItem> ProjectHistory { get; set; } = new ObservableCollection<ProjectHistoryItem>();

        /// <summary>
        /// 项目历史
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ProjectHistoryItem> ProjectHistoryView { get; set; } = new ObservableCollection<ProjectHistoryItem>();
        /// <summary>
        /// 查询命令
        /// </summary>
        [JsonIgnore]
        public ICommand SearchProjectCommand { get; set; }
        /// <summary>
        /// 打开项目命令
        /// </summary>
        [JsonIgnore]
        public ICommand OpenProjectCommand { get; set; }
        /// <summary>
        /// 创建项目命令
        /// </summary>
        [JsonIgnore]
        public ICommand CreateProjectCommand { get; set; }
        /// <summary>
        /// 上一步命令
        /// </summary>
        [JsonIgnore]
        public ICommand PreviousStepCommand { get; set; }
        /// <summary>
        /// 确定创建项目
        /// </summary>
        [JsonIgnore]
        public ICommand SureCreateProjectCommand { get; set; }
        /// <summary>
        /// 打开目录命令
        /// </summary>
        [JsonIgnore]
        public ICommand OpenDirCommand { get; set; }
        /// <summary>
        /// 关闭窗口
        /// </summary>
        [JsonIgnore]
        public ICommand CloseWindowCommand { get; set; }
        /// <summary>
        /// 打开外部项目文件命令
        /// </summary>
        [JsonIgnore]
        public ICommand OpenOtherProjectFileCommand { get; set; }
        /// <summary>
        /// 单独打开一个文件夹 (当没有 项目文件的时候使用)
        /// </summary>
        [JsonIgnore]
        public ICommand OpenOtherProjectFolderCommand { get; set; }
        /// <summary>
        /// 选中的项
        /// </summary>
        [JsonIgnore]
        public ProjectHistoryItem SelectedItem { get; set; }
        /// <summary>
        /// 正在创建的对象
        /// </summary>
        [JsonIgnore]
        public ProjectHistoryItem CreateProjectItem { get; set; } = new ProjectHistoryItem();
        /// <summary>
        /// 主模型
        /// </summary>
        [JsonIgnore]
        public MainModel MainModel { get; set; }
        /// <summary>
        /// 页面序号
        /// </summary>
        [JsonIgnore]
        public int PageIndex { get; set; } = 0;
        /// <summary>
        /// 当前输入的名称
        /// </summary>
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
            // 打开项目
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

            // 创建新项目
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

            // 上一步
            PreviousStepCommand = new SampleCommand(o => true, o => {
                PageIndex = 0;
            });

            // 确认创建项目
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

            // 打开目录命令
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

            // 关闭窗口
            CloseWindowCommand = new SampleCommand(o => true, o => {
                try
                {
                    ProjectOptionWindow.Instance.Close();
                }
                catch (Exception)
                {
                    // Handle exception
                }
            });

            // 打开其他项目文件命令
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

            // 打开其他项目文件夹
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
        /// <summary>
        /// 加载单例
        /// </summary>
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
        /// <summary>
        /// 保存到本地配置
        /// </summary>
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
