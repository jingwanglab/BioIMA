using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf522.Models
{
    /// <summary>
    /// 项目历史项
    /// </summary>
    public class ProjectHistoryItem : INotifyPropertyChanged
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// 项目配置文件的路径
        /// </summary>
        public string ProjectPath { get; set; }
        /// <summary>
        /// 项目目录
        /// </summary>
        public string ProjectDir { get; set; }
        /// <summary>
        /// 保存目标数据的地址
        /// </summary>
        public string SaveTargetDataDir { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
