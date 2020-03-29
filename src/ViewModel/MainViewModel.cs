using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MaterialDesignThemes.Wpf;
using Renci.SshNet;
using SynoDownloader.Core;
using SynoDownloader.Models;
using SynoDownloader.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynoDownloader.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<TreeNode> _directoriesArbo;
        public ObservableCollection<TreeNode> DirectoriesArbo
        {
            get { return _directoriesArbo; }
            set { Set(ref _directoriesArbo, value); }
        }

        private TreeNode _selectedDirectory;
        public TreeNode SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                Set(ref _selectedDirectory, value);
            }
        }

        private DownloadFile _currentDownloadFile;
        public DownloadFile CurrentDownloadFile
        {
            get { return _currentDownloadFile; }
            set { Set(ref _currentDownloadFile, value); }
        }

        private ObservableCollection<DownloadFile> _downloads;
        public ObservableCollection<DownloadFile> Downloads
        {
            get { return _downloads; }
            set { Set(ref _downloads, value); }
        }

        private string _linksInput;
        public string LinksInput
        {
            get { return _linksInput; }
            set { Set(ref _linksInput, value); }
        }

        private SnackbarMessageQueue _foldersSnackbarQueue;
        public SnackbarMessageQueue FoldersSnackbarQueue
        {
            get { return _foldersSnackbarQueue; }
            set { Set(ref _foldersSnackbarQueue, value); }
        }

        public MainViewModel()
        {
            InitializeDataAsync().ConfigureAwait(false);
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        private async Task InitializeDataAsync()
        {
            await Task.Yield();
            var directories = GetRemoteDirectories("/volume1/video/");
            DirectoriesArbo = new AsyncObservableCollection<TreeNode>() { directories };
            Downloads = new ObservableCollection<DownloadFile>();
            directories.IsSelected = true;
            SelectedDirectory = directories;
            FoldersSnackbarQueue = new SnackbarMessageQueue();
        }

        private TreeNode GetRemoteDirectories(string rootPath)
        {
            var sshDirectories = ExecuteSshCommand($"find {rootPath} -type d");
            var filtered = sshDirectories.Split("\n".ToArray(), StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).Where(s => s != rootPath && !s.Contains("@eaDir")).ToList();
            string previousPath = string.Empty;
            foreach (var item in filtered.ToList())
            {
                if (!string.IsNullOrEmpty(previousPath) && item.Contains(previousPath))
                {
                    filtered.Remove(previousPath);
                }
                previousPath = item;
            }
            var rootNode = new TreeNode(rootPath, rootPath, null, isRoot: true, isExpanded: true);
            GetNodesCollection(filtered, rootNode);
            return rootNode;
        }

        private void GetNodesCollection(List<string> directories, TreeNode node)
        {
            var rootPath = node.Path;
            foreach (var item in directories)
            {
                var currentFullPath = item.Replace(rootPath, string.Empty);
                var splitted = currentFullPath.Split('/').ToList();
                TreeNode previousNode = node;
                for (int i = 0; i < splitted.Count; i++)
                {
                    string constructedPath = "";
                    for (int x = 0; x <= i; x++)
                    {
                        constructedPath += $"{splitted[x]}/";
                    }
                    TreeNode nono = previousNode?.Childs?.FirstOrDefault(c => c.DisplayName == splitted[i])
                        ?? new TreeNode(splitted[i], rootPath + constructedPath, previousNode);
                    if (!previousNode.Childs.Any(c => c.DisplayName == nono.DisplayName))
                    {
                        previousNode.Childs.Add(nono);
                    }
                    previousNode = nono;
                }
            }
        }

        private string ExecuteSshCommand(string command)
        {
            var ip = ConfigurationManager.AppSettings["serverIp"];
            var port = ConfigurationManager.AppSettings["serverPort"];
            var user = ConfigurationManager.AppSettings["user"];
            var pwd = ConfigurationManager.AppSettings["pwd"];

            using (SshClient sshClient = new SshClient(ip, int.Parse(port), user, pwd))
            {
                sshClient.Connect();
                try
                {
                    SshCommand scmd = sshClient.CreateCommand(command);
                    var result = scmd.Execute();
                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private async Task SshDownload(List<DownloadFile> dwnFiles)
        {
            await Task.Yield();
            var ip = ConfigurationManager.AppSettings["serverIp"];
            var port = ConfigurationManager.AppSettings["serverPort"];
            var user = ConfigurationManager.AppSettings["user"];
            var pwd = ConfigurationManager.AppSettings["pwd"];

            using (SshClient sshClient = new SshClient(ip, int.Parse(port), user, pwd))
            {
                sshClient.Connect();
                foreach (var item in dwnFiles)
                {
                    try
                    {
                        item.IsDownloading = true;
                        DownloadCommand.RaiseCanExecuteChanged();
                        SshCommand scmd = sshClient.CreateCommand($"wget -P {item.OutputDir} {item.Url}");
                        item.DownloadSshCommand = sshClient;
                        var result = scmd.BeginExecute();

                        using (var reader = new StreamReader(scmd.ExtendedOutputStream, Encoding.UTF8, true, 1024, true))
                        {
                            while (!result.IsCompleted || !reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                if (line != null && !line.Contains("http") && line.Contains("%") && line.Length > 10)
                                {
                                    var percent = line.Substring(line.IndexOf('%') - 2, 2);
                                    var speed = line.Substring(line.IndexOf('%') + 1, line.Length - (line.IndexOf('%') + 1));
                                    item.Percentage = int.Parse(percent.Trim());
                                    item.DownloadSpeed = speed;
                                }
                            }
                        }

                        scmd.EndExecute(result);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        item.IsDownloading = false;
                    }
                }
                sshClient.Disconnect();
            }
            DownloadCommand.RaiseCanExecuteChanged();
        }

        private RelayCommand _downloadCommand;
        public RelayCommand DownloadCommand
        {
            get { return _downloadCommand ?? (_downloadCommand = new RelayCommand(DownloadFile, () => !Downloads?.Any(d => d.IsDownloading) ?? true)); }
        }

        private async void DownloadFile()
        {
            if (!string.IsNullOrEmpty(LinksInput))
            {
                string[] filterSplit = new string[] { "\r", "\n", Environment.NewLine };
                var splitted = LinksInput.Split(filterSplit, StringSplitOptions.RemoveEmptyEntries);
                splitted.ToList().ForEach((l) => Downloads.Add(new DownloadFile(l, SelectedDirectory.Path)));
                await Task.Factory.StartNew(() => SshDownload(Downloads.ToList()));

                //CurrentDownloadFile = new DownloadFile(LinksInput, SelectedDirectory.Path);
                //Downloads.Add(CurrentDownloadFile);
                //
            }

        }

        private RelayCommand _stopDownloadCommand;
        public RelayCommand StopDownloadCommand
        {
            get { return _stopDownloadCommand ?? (_stopDownloadCommand = new RelayCommand(StopDownload)); }
        }

        private void StopDownload()
        {
            if (CurrentDownloadFile != null)
            {
                CurrentDownloadFile.DownloadSshCommand.Disconnect();
                CurrentDownloadFile.DownloadSshCommand.Dispose();
            }
        }

        private RelayCommand<TreeNode> _addSubNodeCommand;
        public RelayCommand<TreeNode> AddSubNodeCommand
        {
            get { return _addSubNodeCommand ?? (_addSubNodeCommand = new RelayCommand<TreeNode>((n) => AddSubNode(n))); }
        }

        private async void AddSubNode(TreeNode node)
        {
            var vm = new AddSubFolderViewModel();

            var view = new AddSubFolderDialog
            {
                DataContext = vm
            };

            //show the dialog
            var dialogResult = await DialogHost.Show(view, "RootDialog");
            if ((bool?)dialogResult ?? false)
            {
                var newFolderName = vm.NewFolderName;
                SelectedDirectory.IsSelected = false;
                if (!node.Childs.Any(c => c.DisplayName == newFolderName))
                {
                    var newChild = new TreeNode(newFolderName, $"{node.Path}{newFolderName}", node);
                    node.Childs.Add(newChild);
                    await newChild.Select();
                    SelectedDirectory = newChild;
                    FoldersSnackbarQueue.Enqueue($"Dossier {newFolderName} créé avec succès");
                }
                else
                {
                    var existingNode = node.Childs.First(c => c.DisplayName == newFolderName);
                    await existingNode.Select();
                    SelectedDirectory = existingNode;
                    FoldersSnackbarQueue.Enqueue($"Le dossier {newFolderName} existe déjà");
                }
            }
            else
            {
                Debug.WriteLine("Dialog d'ajout de dossier fermée avec cancel");
            }
        }

        private RelayCommand<TreeNode> _deleteNodeCommand;
        public RelayCommand<TreeNode> DeleteNodeCommand
        {
            get { return _deleteNodeCommand ?? (_deleteNodeCommand = new RelayCommand<TreeNode>((n) => DeleteNode(n))); }
        }

        private void DeleteNode(TreeNode node)
        {
            var parent = node.Parent;
            SelectedDirectory = parent;
            parent.Childs.Remove(node);
        }
    }
}