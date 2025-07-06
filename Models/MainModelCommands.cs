using wpf522.CustomCommand;
using wpf522.CustomDialogs;
using wpf522.Expends;
using wpf522.Models.DrawShapes;
using wpf522.Models.Enums;
using Interface.Expends;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace wpf522.Models
{
    public partial class MainModel
    {



        public ICommand OpenFolderCommand { get; set; }



        public ICommand SetDrawBoxCommand { get; set; }



        public ICommand SetDrawSelectedCommand { get; set; }



        public ICommand SetDrawLinesCommand { get; set; }




        public ICommand SetDrawPolygonCommand { get; set; }



        public ICommand SetDrawNoneCommand { get; set; }



        public ICommand OpenInputKeySetsCommand { get; set; }



        public ICommand SaveConfigCommand { get; set; }



        public ICommand RemoveSelectedShapesCommand { get; set; }



        public ICommand ImageMoveCommand { get; set; }



        public ICommand CreateTypeCommand { get; set; }



        public ICommand SureCreateTypeCommand { get; set; }



        public ICommand ExportYoloListCommand { get; set; }



        public ICommand RemoveSelectedShapeTypeColorStructCommand { get; set; }



        public ICommand RemoveSelectedShapeAreaCommand { get; set; }



        public ICommand OpenModifyShapeTypeNameCommand { get; set; }



        public ICommand OpenModifyShapeTypeColorPopupCommand { get; set; }



        public ICommand ModifySaveDirCommand { get; set; }



        public ICommand RemoveFileCommand { get; set; }



        public ICommand SaveAllCommand { get; set; }



        public ICommand ConvertPolygonToRectCommand { get; set; }



        public ICommand OpenImageConfigFileCommand { get; set; }



        public ICommand ConvertTypesCommand { get; set; }



        public ICommand SetAngleMeasureCommand { get; set; }



        public ICommand ColorButtonCommand { get; set; }




        public ICommand SetRulerCommand { get; set; }



        private MainModel()
        {
            PropertyChanged += MainModel_PropertyChanged;

            OpenFolderCommand = new SampleCommand(o => true, async o => {

                try
                {
                    FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog(MainWindow.Instance, DirectoryItem.Dir);
                    await MainWindow.Instance.ShowMetroDialogAsync(folderBrowserDialog);
                    await folderBrowserDialog.WaitForResult();
                    if (folderBrowserDialog.OptionResult == true)
                    {
                        LoadImages(folderBrowserDialog.SelectedDirectoryPath);
                    }
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", "Failed to open folder: " + ex.Message);
                }
            });
            SetDrawBoxCommand = new SampleCommand(o => true, o => {
                ToolConfig.CurrentShapType = ShapeType.Box;
                ToolConfig.IsColorPicking = false; 
            });
            SetDrawPolygonCommand = new SampleCommand(o => true, o => {
                ToolConfig.CurrentShapType = ShapeType.Polygon;
                ToolConfig.IsColorPicking = false; 
            });
            SetDrawLinesCommand = new SampleCommand(o => true, o => {
                ToolConfig.CurrentShapType = ShapeType.Lines;
                ToolConfig.IsColorPicking = false; 
            });
            SetDrawSelectedCommand = new SampleCommand(o => true, o =>
            {
                ToolConfig.CurrentShapType = ShapeType.Selected;
                ToolConfig.IsColorPicking = false; 
            });
            SetDrawNoneCommand = new SampleCommand(o => true, o => { ToolConfig.CurrentShapType = ShapeType.None; });

            SetAngleMeasureCommand = new SampleCommand(o => true, o => 
            {
                ToolConfig.CurrentShapType = ShapeType.Angle;
                ToolConfig.IsColorPicking = false; 
            });

            ColorButtonCommand = new SampleCommand(o => true, o =>
            {
                ToolConfig.CurrentShapType = ShapeType.ColorPicker;
                ToolConfig.IsColorPicking = true; 
            });

            OpenInputKeySetsCommand = new SampleCommand(o => true, o =>
            {
                var fly = o as Flyout;
                fly.IsOpen = true;
            });
            SaveConfigCommand = new SampleCommand(o => true, async o => {
                try
                {
                    if (string.IsNullOrEmpty(ToolConfig.SaveFileConfigHistory) || Directory.Exists(ToolConfig.SaveFileConfigHistory) == false)
                    {
                        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog(MainWindow.Instance, DirectoryItem.Dir);
                        await MainWindow.Instance.ShowMetroDialogAsync(folderBrowserDialog);
                        await folderBrowserDialog.WaitForResult();
                        if (folderBrowserDialog.OptionResult == true)
                        {
                            ToolConfig.SaveFileConfigHistory = folderBrowserDialog.SelectedDirectoryPath;
                        }
                    }
                    this.SaveConfig();

                    if (this.CurrentImageModel is not null && this.CurrentImageModel.Shapes.Count > 0)
                        this.CurrentImageModel?.SaveConfig(ToolConfig);
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", "Save failed: " + ex.Message);
                }
            });

            ModifySaveDirCommand = new SampleCommand(o => true, async o => {
                try
                {
                    FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog(MainWindow.Instance, DirectoryItem.Dir);
                    await MainWindow.Instance.ShowMetroDialogAsync(folderBrowserDialog);
                    await folderBrowserDialog.WaitForResult();
                    if (folderBrowserDialog.OptionResult == true)
                    {
                        ToolConfig.SaveFileConfigHistory = folderBrowserDialog.SelectedDirectoryPath;
                    }
                    else
                    {
                        return;
                    }
                    MainWindow.Instance.ShowWait();
                    this.SaveConfig();
                    await UpdateNewSaveConfigPathConfigData(ToolConfig.SaveFileConfigHistory);
                    UpdateShapeAndColor();
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", "Save failed: " + ex.Message);
                }
                finally
                {
                    MainWindow.Instance.CloseWait();
                }
            });

            RemoveSelectedShapesCommand = new SampleCommand(o => true, o => {
                try
                {
                    if (CurrentImageModel == null) return;
                    var removeList = new List<ShapeArea>();
                    foreach (var item in CurrentImageModel.Shapes)
                    {
                        if (item.IsSelected)
                        {
                            removeList.Add(item);
                        }
                    }
                    foreach (var item in removeList)
                    {
                        CurrentImageModel.Shapes.Remove(item);
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.Instance.ShowMessageAsync("Error!", "Delete failed: " + ex.Message); ;
                }
            });

            ImageMoveCommand = new SampleCommand(o => true, o => {
                try
                {
                    int move = o.To<int>();

                    if (TabViewSelectedIndex == 0)
                    {
                        SelectedIndex += move;
                        if (SelectedIndex < 0) SelectedIndex = 0;
                        if (SelectedIndex >= this.ImageModels.Count) SelectedIndex = ImageModels.Count - 1;
                    }

                    else
                    {
                        if (CurrentImageModel is null) return;
                        var parent = CurrentImageModel.ParentNode;
                        if (parent is null)
                        {
                            var index = ImageStructModels.IndexOf(CurrentImageModel) + move;
                            index = Math.Min(index, ImageStructModels.Count - 1);
                            CurrentImageModel = ImageStructModels[index];
                        }
                        else
                        {
                            var index = parent.Children.IndexOf(CurrentImageModel) + move;
                            index = Math.Min(index, parent.Children.Count - 1);
                            CurrentImageModel = parent.Children[index];
                        }
                    }
                }
                catch (Exception)
                { }
            });

            CreateTypeCommand = new SampleCommand(o => true, o => {
                Popup p = MainWindow.Instance.CreateTypePopup;
                string name = "label1";
                while (ToolConfig.ShapeTypeColorStructs.Where(p => p.TypeName.Equals(name + NameIndex)).Count() > 0)
                {
                    NameIndex++;

                }

                CreateTypeStruct = new ShapeTypeColorStruct()
                {
                    TypeName = name + NameIndex,
                    Color = ToolConfig.GetRandomColor().ToString(),
                };
                p.IsOpen = true;
            });
            SureCreateTypeCommand = new SampleCommand(o => true, o => {
                Popup p = MainWindow.Instance.CreateTypePopup;
                p.IsOpen = false;
                if (ToolConfig.ShapeTypeColorStructs.Where(p => p.TypeName.Equals(CreateTypeStruct.TypeName)).Count() > 0)
                {
                    MainWindow.Instance.ShowMessageAsync("Error!", "label already exists!");
                    return;
                }
                else
                {
                    this.ToolConfig.ShapeTypeColorStructs.Add(CreateTypeStruct);
                    CreateTypeStruct = null;
                }
            });

            OpenModifyShapeTypeNameCommand = new SampleCommand(o => true, o => {
                if (MainWindow.Instance is null) return;
                Popup p = MainWindow.Instance.TypeNameChangePopup;
                if (p is not null)
                {
                    p.IsOpen = true;
                    p.StaysOpen = false;
                }
            });

            OpenModifyShapeTypeColorPopupCommand = new SampleCommand(o => true, o => {
                if (MainWindow.Instance is null) return;
                MainWindow.Instance.ModifyShapeTypeColorStructNamePopup.IsOpen = true;

            });

            RemoveSelectedShapeTypeColorStructCommand = new SampleCommand(o => true, o => {
                if (ToolConfig.CurrentTypeName is not null)
                {
                    ToolConfig.ShapeTypeColorStructs.Remove(ToolConfig.CurrentTypeName);
                    ToolConfig.CurrentTypeName = ToolConfig.ShapeTypeColorStructs.FirstOrDefault();
                }
            });

            RemoveSelectedShapeAreaCommand = new SampleCommand(o => true, o => {
                if (CurrentImageModel is not null && CurrentImageModel.SelectedShape is not null)
                {
                    CurrentImageModel.Shapes.Remove(CurrentImageModel.SelectedShape);
                }
            });

            SaveAllCommand = new SampleCommand(o => true, async o => {
                try
                {
                    bool withNotBoard = o.Equals("Include unselected items");
                    if (string.IsNullOrEmpty(ToolConfig.SaveFileConfigHistory) || Directory.Exists(ToolConfig.SaveFileConfigHistory) == false)
                    {
                        var dir = await MainWindow.Instance.OpenFolderDialogAsync();
                        if (dir != null)
                            ToolConfig.SaveFileConfigHistory = dir;
                    }

                    MainWindow.Instance.ShowWait();
                    await Task.Factory.StartNew(async () => {
                        try
                        {
                            this.SaveConfig();

                            foreach (var item in ImageModels)
                            {
                                if (item.Shapes.Count > 0 || withNotBoard)
                                {
                                    item.SaveConfig(ToolConfig);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await MainWindow.Instance.ShowMessageAsync("Save Error", ex.Message);
                        }
                    });
                    MainWindow.Instance.CloseWait();
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", "Save failed: " + ex.Message);
                }
            });

            OpenImageConfigFileCommand = new SampleCommand(o => true, o => {
                try
                {
                    if (this.CurrentImageModel == null) return;
                    if (this.CurrentImageModel.ConfigFilePath is null) return;
                    if (File.Exists(this.CurrentImageModel.ConfigFilePath) == false) return;
                    Process.Start("explorer.exe", this.CurrentImageModel.ConfigFilePath);
                }
                catch (Exception ex)
                {

                }
            });

            ExportYoloListCommand = new SampleCommand(o => ToolConfig.SaveFileConfigHistory != null, async o => {
                try
                {
                    MainWindow.Instance.ShowWait();
                    var dir = await MainWindow.Instance.OpenFolderDialogAsync();
                    if (dir == null) return;

                    await Task.Factory.StartNew(() => {
                        try
                        {
                            var imagePath = System.IO.Path.Combine(dir, "images/train2017").CheckPath();
                            var labels = System.IO.Path.Combine(dir, "labels/train2017").CheckPath();
                            foreach (var item in ImageModels)
                            {
                                if (item.ShapeCount == 0) continue;
                                var randomName = Guid.NewGuid().ToString().Replace("-", "_");
                                var fileName = Path.Combine(imagePath, randomName + Path.GetExtension(item.FilePath));
                                File.Copy(item.GetAbsolutePath(ToolConfig), fileName);
                                var txtPath = Path.Combine(labels, randomName + ".txt");
                                using (FileStream stream = File.Open(txtPath, FileMode.Create))
                                {
                                    foreach (var shape in item.Shapes)
                                    {
                                        if (shape is ShapeBox box)
                                        {
                                            var center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
                                            string content = string.Format("{0} {1} {2} {3} {4}\n", ToolConfig.GetTypeIndex(shape), center.X / item.ImageWidth, center.Y / item.ImageHeight, box.Width / item.ImageWidth, box.Height / item.ImageHeight);
                                            var bs = Encoding.UTF8.GetBytes(content);
                                            stream.Write(bs);
                                        }
                                        else if(shape is ShapePolygon polygon)
                                        {
                                            string content = ToolConfig.GetTypeIndex(shape).ToString() + " " + polygon.Points.JoinToString(" ", p => "{0} {1}".Format((p.X + polygon.StartX) / item.ImageWidth, (p.Y + polygon.StartY) / item.ImageHeight)) + "\n";
                                            var bs = Encoding.UTF8.GetBytes(content);
                                            stream.Write(bs);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });

                    MainWindow.Instance.CloseWait();
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", ex.Message);
                }
            });

            RemoveFileCommand = new SampleCommand(o => CurrentImageModel != null, async o => {
                try
                {
                    var path = CurrentImageModel.GetAbsolutePath(ToolConfig);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        ImageModels.Remove(CurrentImageModel);
                        if(CurrentImageModel.ParentNode != null)
                        {
                            CurrentImageModel.ParentNode.RemoveChild(CurrentImageModel);
                        }
                        else
                        {
                            ImageStructModels.Remove(CurrentImageModel);
                        }
                        CurrentImageModel = null;
                    }

                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", ex.ToString());
                }
            });


            ConvertPolygonToRectCommand = new SampleCommand(o => true, async o => {
                try
                {
                    var dir = await MainWindow.Instance.OpenFolderDialogAsync();
                    if (dir == null) return;

                    MainWindow.Instance.ShowWait();

                    await Task.Factory.StartNew(() =>
                    {
                        string forntPath = ToolConfig.SaveFileConfigHistory;
                        try
                        {
                            ToolConfig.SaveFileConfigHistory = dir;
                            foreach (var item in ImageModels)
                            {
                                ImageContentInfoModel model = new ImageContentInfoModel();
                                model.CopyFrom(item);
                                model.FilePath = item.FilePath;
                                model.ImageUri = item.ImageUri;
                                model.ConvertPolygonToRect();

                                model.ConfigFilePath = Path.Combine(dir, Path.GetFileNameWithoutExtension(model.FilePath) + ".json");
                                model.MaskFilePath = null;
                                model.SaveConfig(ToolConfig, false);
                            }
                        }
                        catch (Exception)
                        {

                        }
                        finally
                        {
                            ToolConfig.SaveFileConfigHistory = forntPath;
                        }
                    });

                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error!", ex.ToString());
                }
                finally
                {
                    MainWindow.Instance.CloseWait();
                }
            });

            ConvertTypesCommand = new SampleCommand(o => true, async o => {
                try
                {
                    await MainWindow.Instance.OpenConverterTypesDialogAsync(this);
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync("Error", ex.ToString());
                }
            });
        }




        public MainModel(ProjectHistoryItem history) : this()
        {
            ConfigSavePath = history.ProjectPath;

            if (File.Exists(ConfigSavePath))
            {
                this.LoadConfig();
            }
            else
            {
                this.LoadImages(history.ProjectDir);
                ToolConfig.SaveFileConfigHistory = history.SaveTargetDataDir;
                ToolConfig.ProjectName = history.ProjectName;
            }
        }




        public MainModel(string configPath) : base()
        {
            if (!File.Exists(configPath)) throw new Exception("Project file does not exist");
            this.ConfigSavePath = configPath;
            this.LoadConfig();
        }





        public static MainModel CreateMainModelByFolder(string dir)
        {
            if (Directory.Exists(dir) == false) throw new Exception("Directory does not exist...");
            MainModel model = new MainModel();
            model.LoadImages(dir);
            model.ToolConfig.ProjectName = Path.GetFileNameWithoutExtension(dir);
            model.ConfigSavePath = Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(dir) + ProjectManager.ProjectConfigDefaultFileExName);
            return model;
        }




        private async Task UpdateNewSaveConfigPathConfigData(string dir)
        {
            await Task.Factory.StartNew(async root => {
                try
                {
                    var files = Directory.EnumerateFiles((string)root, "*.json", SearchOption.AllDirectories);
                    foreach (var item in files)
                    {
                        var config = JsonConvert.DeserializeObject<ImageContentInfoModel>(File.ReadAllText(item, Encoding.UTF8), new ShapeAreaJsonConverter());
                        if (config == null) continue;
                        var find = ImageModels.Where(p => config.FilePath.EndsWith(p.FilePath) || p.FilePath.EndsWith(config.FilePath));
                        if (find.Count() > 1)
                        {
                            continue;
                        }
                        var nowConfig = find.FirstOrDefault();

                        nowConfig?.CopyFrom(config);
                    }

                    var dirs = Directory.GetDirectories((string)root);
                    foreach (var item in dirs)
                    {
                        await UpdateNewSaveConfigPathConfigData(item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }, dir);
        }
    
    }
}

