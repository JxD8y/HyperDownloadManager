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
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.Conditions;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download
{
    public static class DownloadManager
    {
        #region DownloadViewManagment
        public static ObservableCollection<DownloadViewModel> DownloadViewModels = new ObservableCollection<DownloadViewModel>();
        public static ObservableCollection<DownloadViewModel> Running = new ObservableCollection<DownloadViewModel>();
        public static ObservableCollection<DownloadViewModel> Paused = new ObservableCollection<DownloadViewModel>();
        public static ObservableCollection<DownloadViewModel> Completed = new ObservableCollection<DownloadViewModel>();
        public static ObservableCollection<DownloadViewModel> ScheduledDownloads = new ObservableCollection<DownloadViewModel>();
        private static Dictionary<int, DownloadViewModel> PendingDownloads = new Dictionary<int, DownloadViewModel>();
        #region GetViews
        public static DownloadSupervisor? GetDownloadSupervisor(int id)
        {
            return DownloadViewModels.Where((i) => { if (i.Id == id) { return true; } else { return false; } }).First().Supervisor;
        }
        public static DownloadViewModel? GetDownloadViewModel(int id)
        {
            var possibles = DownloadViewModels.Where((i) => { if (i.Id == id) { return true; } else { return false; } });
            if (possibles.Count() == 0)
            {
                return null;
            }
            return possibles.First();
        }
        public static DownloadViewModel? GetDownloadViewModel(string name)
        {
            return DownloadViewModels.Where((i) => { if (i.DownloadName == name) { return true; } else { return false; } }).First();
        }
        public static int GetRunningsCount()
        {
            return DownloadViewModels.Where((i) => { if (i.Supervisor.DownloadCore.IsWorking == true) { return true; } else { return false; } }).Count();
        }
        public static int GetUnworkingUncompleted()
        {
            return DownloadViewModels.Where((i) => { if (i.Supervisor.DownloadCore.IsWorking == false && i.Supervisor.DownloadCore.Completed == false) { return true; } else { return false; } }).Count();
        }
        #endregion
        public static int? Create(IOCore IOCore, ConfigViewModel configViewModel, ContainerViewModel containerViewModel)
        {
            DownloadViewModel viewModel = new DownloadViewModel(GlobalSupervisor.GetRandom(int.MaxValue), IOCore, configViewModel, containerViewModel);
            viewModel.Serialized_id = new BsonValue(Guid.NewGuid());
            if (containerViewModel.AddDownload(viewModel))
            {
                viewModel.Serialized_id = new BsonValue(Guid.NewGuid());
                BsonValue? bsonValue = SaveNewDownload(viewModel);
                if (bsonValue == null)
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to save download in Database: {viewModel.DownloadName}");
                    return -1;
                }
                viewModel.Serialized_id = bsonValue;
                DownloadViewModels.Add(viewModel);
                return viewModel.Id;
            }
            else
            {
                return null;
            }
        }
        public static void Remove(int id)
        {
            try
            {
                DownloadViewModel? viewModel = GetDownloadViewModel(id);
                viewModel.IOCore.CloseFile();
                BsonValue bsonId = viewModel.Serialized_id;
                if (bsonId != null)
                {
                    viewModel.Container.DeleteDownload(viewModel);
                    RemoveDownload(bsonId);
                    DownloadViewModels.Remove(viewModel);
                    LogManager.Log(MessageLevel.Info , LogSection.Download, $"Download removed: {viewModel.DownloadName}");

                }
                else
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to Remove download: {viewModel.DownloadName} ,download DBID is null.");
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to remove Download: {ex.Message}"); }
        }
        public static void SetStop(int id)
        {
            GetDownloadViewModel(id)?.Supervisor.Stop();
        }
        public static void SetStart(int id)
        {
            GetDownloadViewModel(id)?.Supervisor.Start();
        }
        public static void SetState(int id, DownloadState state)
        {
            DownloadViewModel? downloadViewModel = GetDownloadViewModel(id);
            if (downloadViewModel != null)
                SetState(downloadViewModel, state);
        }
        public static void SetState(DownloadViewModel downloadViewModel, DownloadState state)
        {
            if (downloadViewModel != null)
            {
                if (downloadViewModel.Current_State == DownloadState.Downloading)
                    Running.Remove(downloadViewModel);
                else if (downloadViewModel.Current_State == DownloadState.Paused)
                    Paused.Remove(downloadViewModel);
                else if (downloadViewModel.Current_State == DownloadState.Completed)
                    Completed.Remove(downloadViewModel);
                else if (downloadViewModel.Current_State == DownloadState.AwaitingOnCondition)
                    ScheduledDownloads.Remove(downloadViewModel);
                if (state == DownloadState.Completed)
                {
                    Completed.Add(downloadViewModel);
                }
                else if (state == DownloadState.Paused)
                {
                    Paused.Add(downloadViewModel);
                }
                else if (state == DownloadState.Downloading)
                {
                    Running.Add(downloadViewModel);
                }
                else if (state == DownloadState.AwaitingOnCondition)
                {
                    ScheduledDownloads.Add(downloadViewModel);
                }
                downloadViewModel.Current_State = state;
            }
        }
        #endregion
        #region DownloadViewRepo
        public static BsonValue? SaveNewDownload(DownloadViewModel downloadViewModel)
        {
            try
            {
                if (downloadViewModel != null)
                {
                    return LitedbRepo<DownloadViewModel>.Add(downloadViewModel, LitedbRepo<DownloadViewModel>.DownloadsColName);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        public static void UpdateDownload(DownloadViewModel downloadViewModel)
        {
            lock (downloadViewModel)
            {
                try
                {
                    LitedbRepo<DownloadViewModel>.Update(downloadViewModel.Serialized_id, downloadViewModel, LitedbRepo<DownloadViewModel>.DownloadsColName);
                }
                catch (Exception ex)
                {
                    LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to Update Download: {ex.Message}");
                }
            }
        }
        public static void RemoveDownload(BsonValue id)
        {
            try
            {
                LitedbRepo<DownloadViewModel>.Remove(id, LitedbRepo<DownloadViewModel>.DownloadsColName);
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Database, $"fail to Remove Download: {ex.Message}");
            }
        }
        public static void ClearDownloads()
        {
            try
            {
                LitedbRepo<DownloadViewModel>.RemoveAll(LitedbRepo<DownloadViewModel>.DownloadsColName);
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to Clear Downloads: {ex.Message}");
            }
        }
        public static bool Exist(int downloadId)
        {
            return DownloadViewModels.Where((download) => download.Id == downloadId).Count() > 0;
        }
        #endregion
        #region DownloadLoaders
        public static void LoadDownloads()
        {
            try
            {
                List<DownloadViewModel> downloadViewModels = LitedbRepo<DownloadViewModel>.Get(LitedbRepo<DownloadViewModel>.DownloadsColName);
                foreach (DownloadViewModel unsafeDownloadView in downloadViewModels)
                {
                    DownloadViewModel? downloadViewModel = transformDownload(unsafeDownloadView);
                    downloadViewModel.Serialized_id = unsafeDownloadView.Serialized_id;
                    setTriggers(downloadViewModel);
                    if (downloadViewModel != null)
                    {
                        DownloadViewModels.Add(downloadViewModel);
                        downloadViewModel.Container.AddDownload(downloadViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Loader, $"Fail to load Downloads: {ex.Message}");
            }
        }
        private static DownloadViewModel? transformDownload(DownloadViewModel? unsafeDownloadView)
        {
            int id = unsafeDownloadView.Id;
            ConfigViewModel? unsafeConfig = unsafeDownloadView.ConfigViewModel;
            IOCore? unsafeIOSupervisor = unsafeDownloadView.IOCore;
            if (!unsafeConfig.IsValid())
            {
                unsafeConfig = new ConfigViewModel();
                LogManager.Log(MessageLevel.Warning, LogSection.Loader, $"fail to load {unsafeDownloadView.DownloadName}'s settings.");
            }
            ContainerViewModel? downloadContainer;
            if (!ContainerManager.ContainerExist(unsafeDownloadView.ContainerId))
            {
                downloadContainer = ContainerManager.MainContainer;
            }
            else
                downloadContainer = ContainerManager.GetContainer(unsafeDownloadView.ContainerId);

            DownloadViewModel downloadViewModel = new DownloadViewModel(id, unsafeIOSupervisor, unsafeConfig, downloadContainer);
            IOCore iOSupervisorView = new IOCore(unsafeIOSupervisor.fileSavePath, unsafeIOSupervisor.isTempFile, unsafeIOSupervisor.fileSize.OriginData, downloadViewModel, unsafeIOSupervisor);
            downloadViewModel.IOCore = iOSupervisorView;
            downloadViewModel.StartTime = unsafeDownloadView.StartTime;
            downloadViewModel.CreationDate = unsafeDownloadView.CreationDate;
            try
            {
                if (IOCore.IsValidInfoCarrier(iOSupervisorView))
                {
                Jmp:
                    if (iOSupervisorView.IoState == IoState.FileOk)
                    {
                        downloadViewModel.IOCore.Icon = IOUtility.GetFileIcon(iOSupervisorView.fileName);
                        long length = iOSupervisorView.fileStream.Length;
                        downloadViewModel.DownloadedSize = new UnitValue(length);
                        if (downloadViewModel.FileSize.Value.OriginData == length)
                        {
                            downloadViewModel.Current_State = DownloadState.Completed;
                        }
                        else { downloadViewModel.Current_State = DownloadState.Paused; }
                        if (downloadViewModel.DownloadedSize.OriginData >= 0)
                        {
                            downloadViewModel.Current_Percent = (float)(((float)length * 100) / downloadViewModel.FileSize.Value.OriginData);
                        }
                        if (downloadViewModel.Current_Percent >= 100)
                        {
                            iOSupervisorView.CloseFile();
                            downloadViewModel.Current_State = DownloadState.Completed;
                        }
                    }
                    else if ((iOSupervisorView.IoState != IoState.FileIsInUse) && File.Exists(iOSupervisorView.fileSavePath))
                    {
                        iOSupervisorView.OpenFile();
                        goto Jmp;
                    }
                    else
                    {
                        downloadViewModel.Icon = IOUtility.GetFileIcon(iOSupervisorView.fileSavePath);
                        downloadViewModel.DownloadedSize = new UnitValue(0);
                        downloadViewModel.Current_State = DownloadState.Paused;
                    }
                    if (unsafeDownloadView.Current_State == DownloadState.AwaitingOnCondition)
                    {
                        downloadViewModel.Current_State = DownloadState.AwaitingOnCondition;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Loader, $"Fail to load download ({downloadViewModel.DownloadName}): {ex.Message}");
            }
            return downloadViewModel;
        }
        private static void setTriggers(DownloadViewModel? downloadViewModel)
        {
            //Loading Download trigger
            if (downloadViewModel.ConfigViewModel.StartConditionInfo.AutoType != Download.Conditions.AutoStartConditionType.Instant || PendingDownloads.ContainsKey(downloadViewModel.Id))
            {
                if (PendingDownloads.ContainsKey(downloadViewModel.Id))
                {
                    DownloadViewModel pendingDownload = PendingDownloads[downloadViewModel.Id];
                    downloadViewModel.Supervisor.StartCondition = new DownloadCompletedCondition(downloadViewModel) { dlState = DownloadState.Completed, DownloadView = downloadViewModel };
                    downloadViewModel.Supervisor.Start();
                    SetState(downloadViewModel.Id, DownloadState.AwaitingOnCondition);
                }
                else
                {
                    SetState(downloadViewModel.Id, DownloadState.Paused);
                    StartCondition? ConditionStart = null;
                    switch (downloadViewModel.ConfigViewModel.StartConditionInfo.AutoType)
                    {
                        case Download.Conditions.AutoStartConditionType.AbsoluteTime:
                            ConditionStart = new AbsoluteTimeStartCondition(downloadViewModel) { StartAt = downloadViewModel.ConfigViewModel.StartConditionInfo.StartAt };
                            downloadViewModel.Supervisor.StartCondition = ConditionStart;
                            downloadViewModel.Supervisor.Start();
                            break;
                        case Download.Conditions.AutoStartConditionType.RelativeTime:
                            ConditionStart = new RelativeTimeStartCondition(downloadViewModel) { StartIn = downloadViewModel.ConfigViewModel.StartConditionInfo.StartIn, CreationTime = downloadViewModel.CreationDate };
                            downloadViewModel.Supervisor.StartCondition = ConditionStart;
                            downloadViewModel.Supervisor.Start();
                            break;
                        case Download.Conditions.AutoStartConditionType.DownloadStateChange:
                            if (GetDownloadViewModel(downloadViewModel.ConfigViewModel.StartConditionInfo.DownloadId) != null)
                            {
                                DownloadViewModel? _pvm = GetDownloadViewModel(downloadViewModel.ConfigViewModel.StartConditionInfo.DownloadId);
                                ConditionStart = new DownloadCompletedCondition(downloadViewModel) { DownloadView = _pvm, dlState = downloadViewModel.ConfigViewModel.StartConditionInfo.dlState };
                                downloadViewModel.Supervisor.StartCondition = ConditionStart;
                                downloadViewModel.Supervisor.Start();
                            }
                            else
                            {
                                PendingDownloads.Add(downloadViewModel.ConfigViewModel.StartConditionInfo.DownloadId, downloadViewModel);
                            }
                            break;
                        //case Download.Conditions.AutoStartConditionType.AllDownloadFinish:
                        //    ConditionStart = new ContainerCompletedCondition(downloadViewModel);
                        //    downloadViewModel.Supervisor.StartCondition = ConditionStart;
                        //    downloadViewModel.Supervisor.Start();
                        //    break;
                    }
                }
            }
            else
            {
                SetState(downloadViewModel.Id, DownloadState.Paused);
            }
        }
        #endregion
    }
}
