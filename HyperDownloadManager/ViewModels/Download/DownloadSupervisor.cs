using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.DownloadDialog;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.Conditions;
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Timer = System.Timers.Timer;

namespace HyperDownloadManager.ViewModels.Download
{

    public class DownloadSupervisor : ViewModel
    {
        #region Properties
        [BsonIgnore]
        public string LastException { get; set; } = "";
        private bool errorOccurred = false;
        [BsonIgnore]
        public bool ErrorOccurred { get { return errorOccurred; } set { errorOccurred = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public IDownloadCore? DownloadCore = null;
        public DownloadType DownloadType { get; set; } = DownloadType.HttpDownload;
        private bool isWorking;
        [BsonIgnore]
        public bool IsWorking { get { return isWorking; } set { isWorking = value; OnPropertyChanged(); } }
        private bool isCompleted;
        [BsonIgnore]
        public bool IsCompleted { get { return isCompleted; } set { isCompleted = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public DownloadViewModel DownloadViewModel { get; set; } = new DownloadViewModel();
        [BsonIgnore]
        public StartCondition? StartCondition { get; set; }
        private IOCore ioSupervisor { get { return DownloadViewModel.IOCore; } }
        private ConfigViewModel ConfigViewModel { get { return DownloadViewModel.ConfigViewModel; } }

        private Timer UnitMaxCheck = new Timer();
        private Timer SpeedTimer = new Timer(1000);
        private Stopwatch ElapsedStopWatch = new Stopwatch();

        private CancellationTokenSource ConditionCancelToken = new CancellationTokenSource();
        private long AgoReceivedBytes = 0;
        private long ReceivedBytes = 0;
        private long LastReceivedBytes = 0;
        private long currentSpeed = 0;
        private bool timersStarted = false;
        private int UnitCheckerInterval = 5;
        private bool speedOn = false;
        private int speedLockTimes = 0;
        private DateTime End;
        private bool IOEventsAssigned = false;
        #endregion
        [BsonCtor]
        public DownloadSupervisor() { }
        public DownloadSupervisor(DownloadViewModel? downloadView)
        {
            if (downloadView != null)
            {
                DownloadViewModel = downloadView;
                UnitMaxCheck.Elapsed += UnitMax_Elapsed;
                UnitMaxCheck.Interval = UnitCheckerInterval * 1000;
                DownloadCore = new HttpDownloadCore(downloadView.ConfigViewModel);
                if (ConfigViewModel == null)
                    throw new NullReferenceException("ConfigViewModel was null");
                ConfigViewModel.ConfigUpdated += ConfigViewModel_ConfigUpdated;
                NetworkUtility.OnNetworkConnectivityChanged += NetworkWatchDog_OnNetworkConnectivityChanged;
                if (ioSupervisor.fileStream != null && !IOEventsAssigned)
                {
                    ioSupervisor.fileStream.OnMaxFile += FileStream_OnMaxFile;
                    ioSupervisor.fileStream.OnStreamTermination += FileStream_OnStreamTermination;
                    IOEventsAssigned = true;
                }
                IsWorking = false;
                IsCompleted = false;
            }
            else
            {
                throw new NullReferenceException("DownloadViewModel was null");
            }
        }

        #region EventHandlers
        bool stopByConnectionChange = false;
        private void NetworkWatchDog_OnNetworkConnectivityChanged(object? sender, bool e)
        {
            if (!e && this.IsWorking)
            {
                this.Stop();
                stopByConnectionChange = true;
            }
            if (e && !this.IsWorking && stopByConnectionChange)
            {
                stopByConnectionChange = false;
                this.Start();
            }
        }
        private void FileStream_OnStreamTermination(object? sender, StreamEventArgs e)
        {
            if (ConfigViewModel.StartConditionInfo.AutoType != AutoStartConditionType.Instant)
            {
                ConditionCancelToken.Cancel();
                return;
            }
            if (DownloadCore != null)
                DownloadCore.Pause();
            StopTimers();
            IsWorking = false;
            DownloadManager.SetState(DownloadViewModel, DownloadState.Paused);
            AlertUser($"download stopped because another part need the stream.", MessageLevel.Warning);
        }

        private void FileStream_OnMaxFile(object? sender, StreamEventArgs e)
        {
            this.Stop();
            AlertUser("Download reached it's maximum file size.", MessageLevel.Info);
        }
        private void ConfigViewModel_ConfigUpdated(object? sender, List<object> e)
        {
            if (ioSupervisor.Resumable)
            {
                this.Stop();
                this.DownloadCore = null;
                this.Start();
            }
        }
        private async void DownloadCore_OnCompleted(object? sender, EventArgs ev)
        {
            DownloadManager.SetState(DownloadViewModel, DownloadState.Completed);
            this.StopTimers();
            try
            {
                ioSupervisor.ClearTempFile();
            }
            catch (Exception ex)
            {
                await DialogManager.ShowMessageBox($"Error occurred while finalizing download: \n{ex.Message}", MessageLevel.Warning, ButtonOrder.OK);
                this.Stop();
            }
            switch (DownloadViewModel.ConfigViewModel.CompleteType)
            {
                case FinishType.None:
                    ShowDialog();
                    break;
                case FinishType.Shutdown:
                    Shutdown();
                    break;
            }
        }
        #endregion
        #region DownloadControls
        private async void DoCondition()
        {
            DownloadState downloadState = DownloadViewModel.Current_State;
            DownloadManager.SetState(DownloadViewModel, DownloadState.AwaitingOnCondition);
            await Task.Run(WaitUntilConditionFinish);
            DownloadViewModel.ConfigViewModel.StartConditionInfo = new StartConditionInfo();
            DownloadManager.SetState(DownloadViewModel, downloadState);
            DownloadManager.UpdateDownload(DownloadViewModel);
        }
        private IoState CheckStreamStatus()
        {
            if (!this.ioSupervisor.isStreamOpen)
            {
                ioSupervisor.OpenFile();
                if (ioSupervisor.IoState != IoState.FileOk && !ioSupervisor.isStreamOpen && ioSupervisor.fileStream == null)
                {
                    return IoState.FileNotOpen;
                }
            }
            long fileSize = ioSupervisor.fileStream.Length;
            if (fileSize == ioSupervisor.fileSize.OriginData)
                return IoState.FileIsComplete;
            else
            {
                return IoState.FileOk;
            }
        }
        private void LunchCore()
        {
            DownloadCore = CoreFactory.CreateDownloadCore(ConfigViewModel, this.DownloadType);
            switch (DownloadType)
            {
                case DownloadType.HttpDownload:
                    if (DownloadCore == null)
                    {
                        AlertUser("DownloadCore was null.", MessageLevel.Error, true);
                        return;
                    }
                    DownloadCore.ResumeSupport = this.ioSupervisor.Resumable;
                    DownloadCore.OnDataReceived += DownloadCore_OnDataReceived;
                    DownloadCore.OnCompleted += DownloadCore_OnCompleted;
                    StartHttpDownload((HttpDownloadCore)DownloadCore);
                    break;
                case DownloadType.FtpDownload:
                    break;
                case DownloadType.TorrentDownload:
                    break;
            }
        }
        public async void Start(bool IgnoreCondition = false)
        {
            if (!this.IsWorking && !ErrorOccurred && this.DownloadViewModel.Current_State != DownloadState.Verifying && this.DownloadViewModel.Current_State != DownloadState.Downloading)
            {
                if (ioSupervisor.fileStream != null && !IOEventsAssigned)
                {
                    ioSupervisor.fileStream.OnMaxFile += FileStream_OnMaxFile;
                    ioSupervisor.fileStream.OnStreamTermination += FileStream_OnStreamTermination;
                    IOEventsAssigned = true;
                }
                if (DownloadViewModel.Current_State == DownloadState.AwaitingOnCondition)
                {
                    if (await PromptUser("This download is scheduled to run.\nDo you want to start it now?"))
                    {
                        ConditionCancelToken.Cancel();
                        ConditionCancelToken = new CancellationTokenSource();
                    }
                }
                if (!IgnoreCondition)
                {
                    this.DoCondition();
                }
                DownloadViewModel.StartTime = DateTime.Now;
                IoState streamStatus = this.CheckStreamStatus();
                if (streamStatus == IoState.FileOk && ioSupervisor.fileStream.Length > 0)
                {
                    DownloadManager.SetState(DownloadViewModel, DownloadState.Verifying);
                    this.LunchCore();
                }
                else if (streamStatus == IoState.FileOk)
                {
                    this.LunchCore();
                }
                else if (streamStatus == IoState.FileIsComplete)
                {
                    DownloadManager.SetState(DownloadViewModel, DownloadState.Completed);
                    if (await PromptUser("This download already completed.\nWant to see?"))
                    {
                        IOUtility.OpenExplorer(ioSupervisor.saveDirectory);
                    }
                    return;
                }
                else if (streamStatus == IoState.FileOk && this.IsCompleted)//this situation happen if and only if the file of download remove when application is running
                {
                    this.LunchCore();
                }
                else
                {
                    AlertUser($"Cannot start download\nfile is in use", MessageLevel.Error, true);
                }
            }
            else if (this.ErrorOccurred)
            {
                if (this.ProcessError())
                {
                    this.ErrorOccurred = false;
                    this.LastException = "";
                    this.Start(IgnoreCondition);
                }
            }
        }
        private async void StartHttpDownload(HttpDownloadCore core)
        {
            try
            {
                long fileSize = ioSupervisor.fileStream.Length;
                bool StartedYet = fileSize > 0;
                if (StartedYet)
                {
                    if (!await this.VerifyByRecursiveCheck())
                    {
                        if (await PromptUser("fail to verify downloaded segments\nStart over?"))
                        {
                            ioSupervisor.ResetFileData();
                            await core.GetFrom(0, ioSupervisor.fileSize.OriginData);
                            DownloadManager.SetState(DownloadViewModel, DownloadState.Downloading);
                            IsWorking = true;
                        }
                        else
                        {
                            this.Reset();
                            DownloadManager.SetState(DownloadViewModel, DownloadState.Error);
                            AlertUser("User ignored corrupted file.", MessageLevel.Warning);
                            return;
                        }
                    }
                    else
                    {
                        await this.DownloadCore.GetFrom(fileSize, ioSupervisor.fileSize.OriginData);
                        DownloadManager.SetState(DownloadViewModel, DownloadState.Downloading);
                        IsWorking = true;
                    }
                }
                else
                {
                    await core.GetFrom(0, ioSupervisor.fileSize.OriginData);
                    DownloadManager.SetState(DownloadViewModel, DownloadState.Downloading);
                    IsWorking = true;
                }
                this.StartTimers();
            }
            catch (Exception ex)
            {
                this.AlertUser(ex.Message, MessageLevel.Error, true);
                DownloadManager.SetState(DownloadViewModel, DownloadState.Error);
                IsWorking = false;
            }
        }
        public void Stop()
        {
            if (DownloadViewModel.Current_State == DownloadState.Downloading)
            {
                if (ConfigViewModel.StartConditionInfo.AutoType != AutoStartConditionType.Instant)
                {
                    ConditionCancelToken.Cancel();
                    return;
                }
                DownloadCore.Pause();
                ioSupervisor.CloseFile();
                StopTimers();
                IsWorking = false;
                DownloadManager.SetState(DownloadViewModel, DownloadState.Paused);
            }
        }
        #region ResetFunctions
        private async void Reset()
        {
            try
            {
                ioSupervisor.ResetSupervisor();
                this.ResetSupervisor();
                this.IsWorking = false;
            }
            catch (Exception ex)
            {
                AlertUser($"An error occurred in reset: {ex.Message}", MessageLevel.Error, true);
                _Cleanse();
            }
        }
        private void _Cleanse()
        {
            ioSupervisor.CloseFile();
            DownloadCore.Pause();
            this.StopTimers();
        }
        public void ResetSupervisor()
        {
            this.StopTimers();
        }
        #endregion
        public async void WaitUntilConditionFinish()
        {
            switch (ConfigViewModel.StartConditionInfo.AutoType)
            {
                case AutoStartConditionType.Instant:
                    return;
                case AutoStartConditionType.AbsoluteTime:
                    StartCondition = new AbsoluteTimeStartCondition(DownloadViewModel)
                    {
                        StartAt = ConfigViewModel.StartConditionInfo.StartAt
                    };
                    await StartCondition.WaitUntilDone(DownloadViewModel, ConditionCancelToken.Token);
                    ConfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.Instant;
                    break;
                case AutoStartConditionType.RelativeTime:
                    StartCondition = new RelativeTimeStartCondition(DownloadViewModel)
                    {
                        StartIn = ConfigViewModel.StartConditionInfo.StartIn
                    };
                    await StartCondition.WaitUntilDone(DownloadViewModel, ConditionCancelToken.Token);
                    ConfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.Instant;
                    break;
                case AutoStartConditionType.DownloadStateChange:
                    StartCondition = new DownloadCompletedCondition(DownloadViewModel)
                    {
                        DownloadView = DownloadManager.GetDownloadViewModel(ConfigViewModel.StartConditionInfo.DownloadId),
                        dlState = DownloadState.Completed
                    };
                    await StartCondition.WaitUntilDone(DownloadViewModel, ConditionCancelToken.Token);
                    ConfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.Instant;
                    break;
                //case AutoStartConditionType.AllDownloadFinish:
                //    StartCondition = new ContainerCompletedCondition(DownloadViewModel)
                //    {
                //        ContainerViewModel = DownloadViewModel.Container
                //    };
                //    await StartCondition.WaitUntilDone(DownloadViewModel, ConditionCancelToken.Token);
                //    ConfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.None;
                //    break;
            }
        }
        #endregion
        #region VerifyingAndErrorProcessing

        private bool ProcessError()
        {
            if (this.ErrorOccurred)
            {
                if (CheckStreamStatus() == IoState.FileNotOpen)
                {
                    AlertUser("File is currently open in another application.", MessageLevel.Error, true);
                    return false;
                }
                if (!NetworkUtility.CheckConnection())
                {
                    AlertUser("No internet connection.", MessageLevel.Error, true);
                    return false;
                }
                return true;
            }
            return true;
        }
        public async Task<bool> VerifyByRecursiveCheck()
        {
            if (ioSupervisor.Resumable)
            {
                long buff = NetworkUtility.BUFFERSIZE;
                bool safeIntegrity = false;
                int chunk = 1;
                long chunks = ioSupervisor.fileStream.Length / buff;
                int maxChunk = 20;
                Dictionary<int, byte[]> corruptedSegments = new Dictionary<int, byte[]>();
                do
                {
                    if (chunk <= maxChunk)
                    {
                        byte[] IOBuffer = new byte[buff];
                        int offset = -1;
                        //reading data from file:
                        if (ioSupervisor.fileStream.Length <= chunk * buff)
                            offset = 0;
                        else if (ioSupervisor.fileStream.Length > chunk * buff)
                            offset = (int)Math.Clamp(ioSupervisor.fileStream.Length - (chunk * buff), int.MinValue, int.MaxValue);//ensure that no overflow occur in long to int conversion
                        if (offset <= 0)
                            break; //the check process reached the eof
                        IOBuffer = ioSupervisor.readFileBytes(offset, (int)buff);
                        byte[]? NetBuffer = new byte[buff];
                        NetBuffer = await DownloadCore.GetBytes(offset, (int)buff);
                        if (NetBuffer == null)
                            return false;
                        if (IOBuffer.SequenceEqual(NetBuffer))
                        {
                            safeIntegrity = true;
                        }
                        else
                        {
                            chunk += 1;
                            DownloadViewModel.Current_Percent = IOUtility.CalculatePercent(chunk, chunks + 5);
                            corruptedSegments.Add(chunk, NetBuffer);//adding the healthy chunk into list
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                while (!safeIntegrity);
                //repairing byte chunks:
                for (int i = 0; i < corruptedSegments.Count; i++)
                {
                    DownloadViewModel.Current_Percent = IOUtility.CalculatePercent(i + 5, corruptedSegments.Count + 5);
                    var segment = corruptedSegments.ElementAt(i);
                    int _chunk = segment.Key;
                    byte[] healthyChunk = segment.Value;
                    if (healthyChunk != null)
                    {
                        int offset = 0;
                        if (ioSupervisor.fileStream.Length <= _chunk * buff)
                            offset = 0;
                        else if (ioSupervisor.fileStream.Length > _chunk * buff)
                            offset = (int)Math.Clamp(ioSupervisor.fileStream.Length - (_chunk * buff), int.MinValue, int.MaxValue);
                        ioSupervisor.writeToFile(healthyChunk, offset, (int)buff);
                    }
                    return true;
                }
                return true;
            }
            else
                return true;
        }
        #endregion
        #region EndDownloadTriggers


        private void ShowDialog()
        {
            GlobalSupervisor.DownloadPage.Dispatcher.Invoke(() =>
            {
                DownloadFinishedDialog endDialog = new DownloadFinishedDialog(DownloadViewModel);
                DialogManager.ShowDialog($"Finished: {DownloadViewModel.DownloadName}", endDialog, DialogMode.Window);
            });
        }
        private async void Shutdown()
        {
            if (DownloadManager.GetRunningsCount() == 0)
            {
                await DialogManager.ShowMessageBox("Your computer will shutdown in 1 Minutes.", MessageLevel.Warning, ButtonOrder.OK, true);
                Process.Start("shutdown", "/s /t 60");
            }
            else
            {
                LogManager.Log(MessageLevel.Warning, LogSection.Download, $"Cannot shutdown: Running Downloads.");
            }
        }
        private void ResumeAll()
        {
            if (DownloadManager.GetUnworkingUncompleted() < 0)
            {
                DownloadViewModel.Container.StartAllDownload();
            }
            else
            {
                LogManager.Log(MessageLevel.Warning, LogSection.Database, $"Cannot resume all Download: no paused download exist.");
            }
        }
        #endregion
        #region TimerCallbacks
        private void DownloadCore_OnDataReceived(object? sender, Download.DataReceivedEventArgs e)
        {
            ReceivedBytes = e.ReceivedBytes;
            DownloadViewModel.DownloadedSize = new UnitValue(ReceivedBytes);
            DownloadViewModel.Current_Percent = IOUtility.CalculatePercent(e.ReceivedBytes, ioSupervisor.fileSize.OriginData);
            try
            {
                this.ioSupervisor.writeToFile(e.Data, e.DataLength);
            }
            catch (Exception ex)
            {
                this.Stop();
                AlertUser(ex.Message, MessageLevel.Error, true);
            }
        }
        public void StartTimers()
        {
            if (!timersStarted)
            {
                SpeedTimer.Elapsed += SpeedTimer_Elapsed;
                SpeedTimer.Start();
                ElapsedStopWatch.Start();
                UnitMaxCheck.Start();
                timersStarted = true;
            }
        }
        public void StopTimers()
        {
            if (timersStarted)
            {
                SpeedTimer.Elapsed -= SpeedTimer_Elapsed;
                SpeedTimer.Stop();
                ElapsedStopWatch.Stop();
                UnitMaxCheck.Stop();
                timersStarted = false;
            }
        }
        private void UnitMax_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if ((DownloadViewModel?.Current_Speed.OriginData - LastReceivedBytes) > 100)
                {
                    speedOn = true;
                    DateTime Std = DateTime.UtcNow;
                    ioSupervisor.Ping = NetworkUtility.GetServerPing(ioSupervisor.Url.Host);
                }
                LastReceivedBytes = DownloadViewModel.Current_Speed.OriginData;
            }
            catch { }
        }

        private void SpeedTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                long _speed = (ReceivedBytes - AgoReceivedBytes);
                if (_speed == 0)
                {
                    speedLockTimes += 1;
                    if (speedLockTimes >= 20)
                    {
                        if (this.DownloadCore.ResumeSupport)
                        {
                            this.Stop();
                            this.Start(false);
                        }
                    }
                }
                else
                {
                    speedLockTimes = 0;
                }
                currentSpeed = _speed;
                AgoReceivedBytes = ReceivedBytes;
                DownloadViewModel.ElapsedTime = ElapsedStopWatch.Elapsed;
                DownloadViewModel.Current_Speed = new UnitValue(currentSpeed);
                int remainTime = (int)((DownloadViewModel.FileSize.Value.OriginData / currentSpeed) - (ReceivedBytes / currentSpeed));
                if (speedOn)
                {
                    DateTime Std = DateTime.UtcNow;
                    End = Std.AddSeconds(remainTime);
                }
                TimeSpan remainingTime = End - DateTime.UtcNow;
                if (remainingTime >= TimeSpan.Zero)
                {
                    DownloadViewModel.RemainingTime = remainingTime;
                }
                DownloadViewModel.DetailPage.ChartUpdate();
            }
            catch { }
        }
        #endregion
        #region Notifications
        public async void AlertUser(string message, MessageLevel level, bool notify = false, bool log = true)
        {
            string Message = $"{level}, Download name: {DownloadViewModel.DownloadName}, {message}";
            if (level == MessageLevel.Error)
            {
                this.ErrorOccurred = true;
                this.LastException = message;
            }
            if (notify)
                await DialogManager.ShowMessageBox(Message, level, ButtonOrder.OK);
            if (log)
                LogManager.Log(level, LogSection.Download, message, true);
        }
        public async Task<bool> PromptUser(string message)
        {
            return (await DialogManager.ShowMessageBox(message, MessageLevel.Info, ButtonOrder.YESNO) == MessageBoxStatus.YES);
        }
        #endregion
    }
}
