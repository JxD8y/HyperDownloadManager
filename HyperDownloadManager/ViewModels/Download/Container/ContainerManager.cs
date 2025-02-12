using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Repository;
using HyperDownloadManager.Utils;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download.Container
{
    public static class ContainerManager
    {
        public const int ContainerMaxCapacity = 50;
        public static ContainerViewModel MainContainer = new ContainerViewModel();
        public static ContainerViewModel? CurrentContainer = MainContainer;
        public static ObservableCollection<ContainerViewModel> Containers = new ObservableCollection<ContainerViewModel>() { };
        public static string DefaultContainerName = "Container-";
        public static event EventHandler<EventArgs>? OnSelectedContainerChanged;
        public static string MainContainerName { get; set; } = "All";
        public static int CreateContainer(string name, BsonValue? sid, int ContainerMax = ContainerMaxCapacity, string savePath = "")
        {
            if (sid == null)
                sid = new BsonValue(Guid.NewGuid());
            lock (sid)
            {
                int id = GetLastContainerIndex() + 1;
                ContainerViewModel container = new ContainerViewModel(name, ContainerMax, id, sid);
                Containers.Add(container);
                container.CreationTime = DateTime.Now;
                container.Serialized_id = new BsonValue(Guid.NewGuid());
                if (savePath != string.Empty)
                {
                    container.Path = savePath;
                }
                else
                {
                    container.Path = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder ?? IOUtility.GetSystemDownloadFolder() ?? "";
                    if (container.Path == "")
                        throw new Exception("cannot find system's default download directory");
                }
                SaveNewContainer(container);
                return id;
            }
        }
        private static int GetLastContainerIndex()
        {
            if (Containers != null && Containers.Count > 0)
            {
                var lastContainer = Containers.Last();
                return lastContainer.Id;
            }
            return 0;
        }
        public static ContainerViewModel CreateContainer(string name, int ContainerMax = ContainerMaxCapacity, string savePath = "")
        {
            lock (new object())
            {
                int id = GetLastContainerIndex() + 1;
                ContainerViewModel container = new ContainerViewModel(name, ContainerMax, id,null);
                container.CreationTime = DateTime.Now;
                container.Serialized_id = new BsonValue(Guid.NewGuid());
                if (savePath != "")
                {
                    container.Path = savePath;
                }
                else
                {
                    container.Path = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder ?? IOUtility.GetSystemDownloadFolder() ?? "";
                    if (container.Path == "")
                        throw new Exception("cannot find system's default download directory");
                }
                SaveNewContainer(container);
                Containers.Add(container);
                return container;
            }
        }
        private static ContainerViewModel LoadContainer(ContainerViewModel viewModel)
        {
            lock (new object())
            {
                ContainerViewModel container = new ContainerViewModel(viewModel.Name, viewModel.MaxCapacity, viewModel.Id, null);
                container.IsMain = viewModel.IsMain;
                container.Path = viewModel.Path;
                container.Description = viewModel.Description;
                container.MaxCapacity = viewModel.MaxCapacity;
                container.MaxOccupied = viewModel.MaxOccupied;
                container.Serialized_id = viewModel.Serialized_id;
                container.CreationTime = viewModel.CreationTime;
                if (container.IsMain)
                {
                    container.Path = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder ?? IOUtility.GetSystemDownloadFolder() ?? "";
                    if (container.Path == "")
                        throw new Exception("cannot find system's default download directory");
                }
                Containers.Add(container);
                return container;
            }
        }
        #region Oprations
        public static bool ContainerExist(string name) { var cn = from container in Containers where container.Name == name select container; return cn.Count() >= 1; }
        public static bool ContainerExist(int id) { var cn = from container in Containers where container.Id == id select container; return cn.Count() >= 1; }
        public static ContainerViewModel GetContainer(int Id) { return Containers.Where((cn) => { if (cn.Id == Id) return true; else return false; }).First(); }
        public static void RemoveContainer(ContainerViewModel container, bool RemoveDownloads = false)
        {
            if (Containers.Contains(container))
            {
                if (!RemoveDownloads)
                {
                    MainContainer.AddRangeNodes(container.Nodes);
                }
                else
                {
                    for(int i =0;i < container.Nodes.Count; i++)
                    {
                        DownloadViewModel node = container.Nodes[i];
                        DownloadManager.Remove(node);
                    }
                }
                Containers.Remove(container);
                RemoveContainer(container.Serialized_id);
            }
        }
        public static void RemoveContainer(int Id, bool RemoveDownloads = false)
        {
            ContainerViewModel container = GetContainer(Id);
            if (Containers.Contains(container))
            {
                if (!RemoveDownloads)
                {
                    MainContainer.AddRangeNodes(container.Nodes);
                }
                else
                {
                    foreach (DownloadViewModel node in container.Nodes)
                    {
                        DownloadManager.Remove(node);
                    }
                }
                Containers.Remove(container);
            }
        }
        public static void DeselectContainer()
        {
            if (CurrentContainer != null)
            {
                CurrentContainer.IsCurrentContainer = false;
                CurrentContainer = null;
            }
        }
        public static void PauseContainerNodes(ContainerViewModel container)
        {
            foreach (DownloadViewModel node in container.Nodes)
            {
                DownloadManager.SetStop(node);
            }
        }
        public static void StartContainerNodes(ContainerViewModel container)
        {
            foreach (DownloadViewModel node in container.Nodes)
            {
                DownloadManager.SetStart(node);
            }
        }
        public static void ChooseContainer(int id)
        {
            ContainerViewModel container = GetContainer(id);
            if (CurrentContainer != null)
                CurrentContainer.IsCurrentContainer = false;
            CurrentContainer = container;
            container.IsCurrentContainer = true;
            if (OnSelectedContainerChanged != null)
                OnSelectedContainerChanged(container, new EventArgs());
        }
        public static void MoveDownload(DownloadViewModel download, ContainerViewModel container)
        {
            if (download != null)
            {
                ContainerViewModel? _container = download.Container;
                if (_container != null)
                {
                    bool deleted = _container.DeleteDownload(download);
                    if (!deleted)
                        download.ContainerId = -1;
                    container.AddDownload(download, true);
                }
                else
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Database, $"fail to move download {download.DownloadName}: download is not in any container");
                }
            }
        }
        #endregion
        #region ContainerRepo
        public static BsonValue? SaveNewContainer(ContainerViewModel viewModel)
        {
            try
            {
                if (viewModel != null)
                {
                    return LitedbRepo<ContainerViewModel>.Add(viewModel, LitedbRepo<ContainerViewModel>.ContainerColName);
                }
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Database, $"Fail to Save Container in DB: {ex.Message}");
                return null;
            }
        }
        public static void UpdateContainer(ContainerViewModel viewModel)
        {
            if(viewModel.Serialized_id == null)
                viewModel.Serialized_id = new BsonValue(Guid.NewGuid());
            lock (viewModel)
            {
                try
                {
                    LitedbRepo<ContainerViewModel>.Update(viewModel.Serialized_id, viewModel, LitedbRepo<ContainerViewModel>.ContainerColName);
                }
                catch (Exception ex)
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Database, $"fail to Update container: {ex.Message}");
                }
            }
        }
        public static void RemoveContainer(BsonValue? id)
        {
            if (id != null)
            {
                try
                {
                    LitedbRepo<ContainerViewModel>.Remove(id, LitedbRepo<ContainerViewModel>.ContainerColName);
                }
                catch (Exception ex)
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Database, $"Fail to Remove container: {ex.Message}");
                }
            }
            else
            {
                LogManager.Log(MessageLevel.Error, LogSection.Database, $"Fail to remove container: Container SID was null");
            }
        }
        public static void ClearContainers()
        {
            try
            {
                LitedbRepo<ContainerViewModel>.RemoveAll(LitedbRepo<ContainerViewModel>.ContainerColName);
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Database, $"Fail to CleanUp Containers: {ex.Message}");
            }
        }
        public static void LoadContainers()
        {
            List<ContainerViewModel> _containers = LitedbRepo<ContainerViewModel>.Get(LitedbRepo<ContainerViewModel>.ContainerColName).OrderBy((i) => i.Id).ToList();
            try
            {
                if (_containers != null && _containers.Count != 0)
                {
                    foreach (ContainerViewModel container in _containers)
                    {
                        ContainerViewModel viewModel = LoadContainer(container);
                        if (viewModel.IsMain)
                        {
                            MainContainer = viewModel;
                            ChooseContainer(viewModel.Id);
                        }
                    }
                }
                else
                {
                    MainContainer = CreateContainer(MainContainerName);
                    MainContainer.IsMain = true;
                    UpdateContainer(MainContainer);
                    ChooseContainer(MainContainer.Id);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Container, $"fail to Load containers: {ex.Message}");
            }
        }
        #endregion
    }
}
