using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Repository;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download.Container
{
    public static class ContainerManager
    {
        public const int ContainerMaxCapacity = 50;
        public static ContainerViewModel? MainContainer = null;
        public static ContainerViewModel? CurrentContainer = MainContainer;
        public static ObservableCollection<ContainerViewModel> Containers = new ObservableCollection<ContainerViewModel>() { };
        public static string DefaultContainerName = "Container-";
        public static event EventHandler<EventArgs>? OnSelectedContainerChanged;
        public static string MainContainerName { get; set; } = "All";
        public static int CreateContainer(string name, BsonValue sid, int ContainerMax = ContainerMaxCapacity, string savePath = "")
        {
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
                    container.Path = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder;
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
                ContainerViewModel container = new ContainerViewModel(name, ContainerMax, id, null);
                container.CreationTime = DateTime.Now;
                container.Serialized_id = new BsonValue(Guid.NewGuid());
                if (savePath != string.Empty)
                {
                    container.Path = savePath;
                }
                else
                {
                    DirectoryInfo directoryInfo = Directory.CreateDirectory(Path.Combine(GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder, name));
                    container.Path = directoryInfo.FullName;
                }
                SaveNewContainer(container);
                Containers.Add(container);
                return container;
            }
        }
        private static ContainerViewModel LoadContainer(ContainerViewModel dcm)
        {
            lock (new object())
            {
                ContainerViewModel container = new ContainerViewModel(dcm.Name, dcm.MaxCapacity, dcm.Id, null);
                container.IsMain = dcm.IsMain;
                container.Path = dcm.Path;
                container.Description = dcm.Description;
                container.MaxCapacity = dcm.MaxCapacity;
                container.MaxOccupied = dcm.MaxOccupied;
                container.Serialized_id = dcm.Serialized_id;
                container.CreationTime = dcm.CreationTime;
                if (container.IsMain)
                {
                    container.Path = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder;
                }
                else
                {
                    container.Path = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder;
                }
                Containers.Add(container);
                return container;
            }
        }
        public static void HighlightDownload(int downloadid, int containerid)
        {
            ContainerViewModel dcm = GetContainer(containerid);
            if (dcm != null)
            {
                dcm.HighLightDownload(downloadid);
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
                    foreach (DownloadViewModel node in container.Nodes)
                    {
                        DownloadManager.Remove(node.Id);
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
                        DownloadManager.Remove(node.Id);
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
                DownloadManager.SetStop(node.Id);
            }
        }
        public static void StartContainerNodes(ContainerViewModel container)
        {
            foreach (DownloadViewModel node in container.Nodes)
            {
                DownloadManager.SetStart(node.Id);
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
                bool deleted = _container.DeleteDownload(download);
                if (!deleted)
                    download.ContainerId = -1;
                container.AddDownload(download, true);
            }
        }
        #endregion
        #region ContainerRepo
        public static BsonValue? SaveNewContainer(ContainerViewModel dcm)
        {
            try
            {
                if (dcm != null)
                {
                    return LitedbRepo<ContainerViewModel>.Add(dcm, LitedbRepo<ContainerViewModel>.ContainerColName);
                }
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Database, $"Fail to Save Container in DB: {ex.Message}");
                return null;
            }
        }
        public static void UpdateContainer(ContainerViewModel dcm)
        {
            lock (dcm)
            {
                try
                {
                    LitedbRepo<ContainerViewModel>.Update(dcm.Serialized_id, dcm, LitedbRepo<ContainerViewModel>.ContainerColName);
                }
                catch (Exception ex)
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Database, $"fail to Update container: {ex.Message}");
                }
            }
        }
        public static void RemoveContainer(BsonValue id)
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
                        ContainerViewModel dcm = LoadContainer(container);
                        if (dcm.IsMain)
                        {
                            MainContainer = dcm;
                            ChooseContainer(dcm.Id);
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
