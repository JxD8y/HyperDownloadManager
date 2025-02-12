using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var list = DownloadViewModels.Where((i) => { if (i.Id == id) { return true; } else { return false; } });
            if (list.Count() == 0)
            {
                return null;
            }
            return list.First();
        }
        public static DownloadViewModel? GetDownloadViewModel(string name)
        {
            return DownloadViewModels.Where((i) => { if (i.DownloadName == name) { return true; } else { return false; } }).First();
        }
        public static int GetRunningsCount()
        {
            return DownloadViewModels.Where((i) => { return i.IsWorking;  }).Count();
        }
        public static int GetUnworkingUncompleted()
        {
            return DownloadViewModels.Where((i) => { if (i.IsWorking == false && i.IsCompleted == false) { return true; } else { return false; } }).Count();
        }
        #endregion

        public static DownloadViewModel Create(DownloadUriInfo downloadInfo,ConfigViewModel configViewModel, ContainerViewModel containerViewModel)
        {
            DownloadViewModel viewModel = new DownloadViewModel(GlobalSupervisor.GetRandom(int.MaxValue), configViewModel, containerViewModel);
            viewModel.Ping = downloadInfo.Ping;

            if (downloadInfo.Url?.OriginalString == null)
                throw new NullReferenceException("empty url passed to create download");

            viewModel.CurrentUrl = downloadInfo.Url?.OriginalString ?? "";
            viewModel.Icon = downloadInfo.Icon;
            viewModel.ResumeSupport = downloadInfo.Resumable;

            if(downloadInfo.SaveDirectory != "")
                viewModel.FileSavePath = Path.Combine(downloadInfo.SaveDirectory, downloadInfo.FileName);
            else
                viewModel.FileSavePath = Path.Combine(containerViewModel.Path, downloadInfo.FileName);

            viewModel.FileSize = downloadInfo.Size;

            viewModel.Serialized_id = new BsonValue(Guid.NewGuid());
            if (containerViewModel.AddDownload(viewModel))
            {
                viewModel.Serialized_id = new BsonValue(Guid.NewGuid());
                BsonValue? id = SaveNewDownload(viewModel);
                if (id == null)
                {
                    throw new Exception($"fail to save download in Database: {viewModel.DownloadName}");
                }
                viewModel.Serialized_id = id;
                DownloadViewModels.Add(viewModel);
                return viewModel;
            }
            else
            {
                throw new Exception("Selected container cannot import download");
            }
        }
        public static void Remove(DownloadViewModel? viewModel)
        {
            try
            {
                if (viewModel == null)
                    throw new ArgumentException("Download does not exist");
                viewModel.Supervisor?.IOCore.CloseFile();
                BsonValue? sId = viewModel.Serialized_id;
                if (sId == null)
                    throw new Exception("download doesn't exist in database");
                if (viewModel.Container != null)
                    viewModel.Container.DeleteDownload(viewModel);

                RemoveDownload(sId);
                DownloadViewModels.Remove(viewModel);
                LogManager.Log(MessageLevel.Info , LogSection.Download, $"Download removed: {viewModel.DownloadName}");
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to remove Download: {ex.Message}"); 
            }
        }
        public static void SetStop(DownloadViewModel? viewModel)
        {
            if (viewModel == null)
                throw new Exception("download doesn't exist");

            viewModel.Supervisor?.Stop();
        }
        public static void SetStart(DownloadViewModel? viewModel)
        {
            if (viewModel == null)
                throw new Exception("download doesn't exist");

            viewModel.Supervisor?.Start();
        }
        public static void SetState(DownloadViewModel? downloadViewModel, DownloadState state)
        {
            if (downloadViewModel == null)
                throw new Exception("download doesn't exist");

            if (downloadViewModel.CurrentState == DownloadState.Downloading)
                Running.Remove(downloadViewModel);

            else if (downloadViewModel.CurrentState == DownloadState.Paused)
                Paused.Remove(downloadViewModel);

            else if (downloadViewModel.CurrentState == DownloadState.Completed)
                Completed.Remove(downloadViewModel);

            else if (downloadViewModel.CurrentState == DownloadState.AwaitingOnCondition)
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

            downloadViewModel.CurrentState = state;
            
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
                    if (downloadViewModel.Serialized_id == null)
                        downloadViewModel.Serialized_id = new BsonValue(new Guid());

                    LitedbRepo<DownloadViewModel>.Update(downloadViewModel.Serialized_id, downloadViewModel, LitedbRepo<DownloadViewModel>.DownloadsColName);
                }
                catch { }
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
                LogManager.Log(MessageLevel.Error, LogSection.Database, $"fail to remove download: {ex.Message}");
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
                LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to clear downloads: {ex.Message}");
            }
        }
        public static bool Exist(int downloadId)
        {
            return DownloadViewModels.Where((download) => download.Id == downloadId).Count() > 0;
        }
        #endregion
        #region DownloadLoader
        public static void LoadDownloads()
        {
            try
            {
                List<DownloadViewModel> downloadViewModels = LitedbRepo<DownloadViewModel>.Get(LitedbRepo<DownloadViewModel>.DownloadsColName);
                foreach (DownloadViewModel unsafeDownloadView in downloadViewModels)
                {
                    DownloadViewModel? downloadViewModel = transformDownload(unsafeDownloadView);
                    if(downloadViewModel != null && downloadViewModel.Container != null)
                    {
                        downloadViewModel.Serialized_id = unsafeDownloadView.Serialized_id;
                        DownloadViewModels.Add(downloadViewModel);
                        downloadViewModel.Container.AddDownload(downloadViewModel);
                        SetTriggers(downloadViewModel);
                    }
                    else
                    {
                        LogManager.Log(MessageLevel.Error, LogSection.Loader, $"fail to load download: {unsafeDownloadView.CurrentFileName}",true);
                    }

                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Loader, $"Fail to load downloads: {ex.Message}");
            }
        }
        private static DownloadViewModel? transformDownload(DownloadViewModel? unsafeDownloadView)
        {
            if (unsafeDownloadView == null)
                return null;

            int id = unsafeDownloadView.Id;

            ConfigViewModel unsafeConfig = unsafeDownloadView.ConfigViewModel ?? new ConfigViewModel();

            if (!unsafeConfig.IsValid())
            {
                unsafeConfig = new ConfigViewModel();
                LogManager.Log(MessageLevel.Warning, LogSection.Loader, $"fail to load {unsafeDownloadView.DownloadName}'s settings.");
            }

            ContainerViewModel downloadContainer;

            if (!ContainerManager.ContainerExist(unsafeDownloadView.ContainerId))
            {
                downloadContainer = ContainerManager.MainContainer;
            }
            else
                downloadContainer = ContainerManager.GetContainer(unsafeDownloadView.ContainerId);

            DownloadViewModel downloadViewModel = new DownloadViewModel(id,unsafeConfig,downloadContainer);
            downloadViewModel.StartTime = unsafeDownloadView.StartTime;
            downloadViewModel.CreationDate = unsafeDownloadView.CreationDate;
            downloadViewModel.FileSavePath = unsafeDownloadView.FileSavePath;
            downloadViewModel.FileSize = unsafeDownloadView.FileSize;
            downloadViewModel.CurrentUrl = unsafeDownloadView.CurrentUrl;
            downloadViewModel.ResumeSupport = unsafeDownloadView.ResumeSupport;

            try
            {
            Jmp:
                if (downloadViewModel.Supervisor?.IOCore.IoState == IoState.FileOk)
                {
                    downloadViewModel.Icon = IOUtility.GetFileIcon(downloadViewModel.CurrentFileName ?? "");
                    long length = downloadViewModel.DownloadedSize.OriginData;

                    if (downloadViewModel.FileSize.OriginData == length)
                    {
                        downloadViewModel.CurrentState = DownloadState.Completed;
                        downloadViewModel.IsCompleted = true;
                    }
                    else 
                    { 
                        downloadViewModel.CurrentState = DownloadState.Paused; 
                    }
                    if (downloadViewModel.DownloadedSize.OriginData >= 0)
                    {
                        downloadViewModel.CurrentPercent = IOUtility.CalculatePercent(length,downloadViewModel.FileSize.OriginData);
                    }
                    if (downloadViewModel.CurrentPercent >= 100)
                    {
                        downloadViewModel.Supervisor.IOCore.CloseFile();
                        downloadViewModel.CurrentState = DownloadState.Completed;
                    }
                }
                else if ((downloadViewModel.Supervisor?.IOCore.IoState != IoState.FileIsInUse) && File.Exists(downloadViewModel.FileSavePath))
                {
                    downloadViewModel.Supervisor?.IOCore.OpenFile();
                    goto Jmp;
                }
                else
                {
                    downloadViewModel.Icon = IOUtility.GetFileIcon(downloadViewModel.FileSavePath ?? "");
                    downloadViewModel.DownloadedSize = new UnitValue(0);
                    downloadViewModel.CurrentState = DownloadState.Paused;
                }
                if (unsafeDownloadView.CurrentState == DownloadState.AwaitingOnCondition)
                {
                    downloadViewModel.CurrentState = DownloadState.AwaitingOnCondition;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Loader, $"Fail to load download ({downloadViewModel.DownloadName}): {ex.Message}");
            }
            return downloadViewModel;
        }
        private static void SetTriggers(DownloadViewModel downloadViewModel)
        {
            if ((downloadViewModel.ConfigViewModel?.StartConditionInfo.ConditionType != AutoStartConditionType.Instant || PendingDownloads.ContainsKey(downloadViewModel.Id)) && downloadViewModel.Supervisor != null)
            {
                if (PendingDownloads.ContainsKey(downloadViewModel.Id))
                {
                    DownloadViewModel pendingDownload = PendingDownloads[downloadViewModel.Id];
                    downloadViewModel.Supervisor.StartCondition = new DownloadCompletedCondition(downloadViewModel) { dlState = DownloadState.Completed, DownloadView = downloadViewModel };
                    downloadViewModel.Supervisor.Start();
                    SetState(downloadViewModel, DownloadState.AwaitingOnCondition);
                }
                else
                {
                    SetState(downloadViewModel, DownloadState.Paused);
                    StartCondition? ConditionStart = null;
                    switch (downloadViewModel.ConfigViewModel?.StartConditionInfo.ConditionType)
                    {
                        case AutoStartConditionType.AbsoluteTime:
                            ConditionStart = new AbsoluteTimeStartCondition(downloadViewModel) { StartAt = downloadViewModel.ConfigViewModel.StartConditionInfo.StartAt };
                            downloadViewModel.Supervisor.StartCondition = ConditionStart;
                            downloadViewModel.Supervisor?.Start();
                            break;
                        case AutoStartConditionType.RelativeTime:
                            ConditionStart = new RelativeTimeStartCondition(downloadViewModel) { StartIn = downloadViewModel.ConfigViewModel.StartConditionInfo.StartIn, CreationTime = downloadViewModel.CreationDate };
                            downloadViewModel.Supervisor.StartCondition = ConditionStart;
                            downloadViewModel.Supervisor.Start();
                            break;
                        case AutoStartConditionType.DownloadStateChange:
                            DownloadViewModel? view = GetDownloadViewModel(downloadViewModel.ConfigViewModel.StartConditionInfo.DownloadId);
                            if (view != null)
                            {
                                ConditionStart = new DownloadCompletedCondition(downloadViewModel) { DownloadView = view, dlState = downloadViewModel.ConfigViewModel.StartConditionInfo.DownloadState };
                                downloadViewModel.Supervisor.StartCondition = ConditionStart;
                                downloadViewModel.Supervisor.Start();
                            }
                            else
                            {
                                PendingDownloads.Add(downloadViewModel.ConfigViewModel.StartConditionInfo.DownloadId, downloadViewModel);
                            }
                            break;
                    }
                }
            }
            else
            {

                SetState(downloadViewModel, downloadViewModel.CurrentState);
            }
        }
        #endregion
    }
}
