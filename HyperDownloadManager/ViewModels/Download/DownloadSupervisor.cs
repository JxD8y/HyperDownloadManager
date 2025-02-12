using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.DownloadDialog;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.Conditions;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Timer = System.Timers.Timer;

namespace HyperDownloadManager.ViewModels.Download
{

    public class DownloadSupervisor
    {
        #region Properties

        public StartCondition? StartCondition { get; set; }
        public IOCore IOCore = new IOCore();
        public IDownloadCore? DownloadCore = null;
        private DownloadViewModel model = new DownloadViewModel();

        private bool _isWorking;
        private bool isWorking { get { return _isWorking; } set { if (this.model != null) { this.model.IsWorking = value; } _isWorking = value; } }
        private bool _isCompleted;
        private bool isCompleted { get { return _isCompleted; } set { if (this.model != null) { this.model.IsCompleted = value; } _isCompleted = value; } }

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
        public DownloadSupervisor() { }
        public DownloadSupervisor(DownloadViewModel? downloadView)
        {
            if (downloadView != null)
            {
                IOCore = new IOCore(downloadView);
                this.model = downloadView;
                UnitMaxCheck.Elapsed += UnitMax_Elapsed;
                UnitMaxCheck.Interval = UnitCheckerInterval * 1000;
                DownloadCore = new HttpDownloadCore(downloadView.ConfigViewModel);
                if (this.model.ConfigViewModel == null)
                    throw new NullReferenceException("ConfigViewModel was null");
                this.model.ConfigViewModel.ConfigUpdated += ConfigViewModel_ConfigUpdated;
                NetworkUtility.OnNetworkConnectivityChanged += NetworkWatchDog_OnNetworkConnectivityChanged;
                if (IOCore.fileStream != null && !IOEventsAssigned)
                {
                    this.IOCore.fileStream.OnMaxFile += FileStream_OnMaxFile;
                    this.IOCore.fileStream.OnStreamTermination += FileStream_OnStreamTermination;
                    IOEventsAssigned = true;
                }
                this.isWorking = false;
                this.isCompleted = false;
            }
            else
            {
                throw new NullReferenceException("downloadViewModel was null");
            }
        }

        #region EventHandlers
        bool stopByConnectionChange = false;
        private void NetworkWatchDog_OnNetworkConnectivityChanged(object? sender, bool e)
        {
            if (!e && this.isWorking)
            {
                this.Stop();
                stopByConnectionChange = true;
            }
            if (e && !this.isWorking && stopByConnectionChange)
            {
                stopByConnectionChange = false;
                this.Start();
            }
        }
        private void FileStream_OnStreamTermination(object? sender, StreamEventArgs e)
        {
            if (this.model?.ConfigViewModel.StartConditionInfo.ConditionType != AutoStartConditionType.Instant)
            {
                ConditionCancelToken.Cancel();
                return;
            }
            if (DownloadCore != null)
                DownloadCore.Pause();
            StopTimers();
            this.isWorking = false;
            DownloadManager.SetState(this.model, DownloadState.Paused);
            AlertUser($"download stopped because another part need the stream.", MessageLevel.Warning);
        }

        private void FileStream_OnMaxFile(object? sender, StreamEventArgs e)
        {
            this.Stop();
            AlertUser("Download reached it's maximum file size.", MessageLevel.Info);
        }
        private void ConfigViewModel_ConfigUpdated(object? sender, List<object> e)
        {
            if(this.model.ResumeSupport)
            {   this.Stop();
                this.DownloadCore = null;
                this.Start();
            }
        }
        private async void DownloadCore_OnCompleted(object? sender, EventArgs ev)
        {
            if (this.model is DownloadViewModel)
            {
                DownloadManager.SetState(this.model, DownloadState.Completed);
                this.StopTimers();
                try
                {
                    IOCore?.ClearTempFile();
                }
                catch (Exception ex)
                {
                    await DialogManager.ShowMessageBox($"Error occurred while finalizing download: \n{ex.Message}", MessageLevel.Warning, ButtonOrder.OK);
                    this.Stop();
                }
                switch (this.model.ConfigViewModel.CompleteType)
                {
                    case FinishType.None:
                        ShowDialog();
                        break;
                    case FinishType.Shutdown:
                        Shutdown();
                        break;
                }
            }
            this.isCompleted = true;
            DownloadManager.SetState(this.model, DownloadState.Completed);
        }
        #endregion
        #region DownloadControls
        private async Task DoCondition()
        {
            if (this.model is DownloadViewModel)
            {
                DownloadState downloadState = this.model.CurrentState;
                DownloadManager.SetState(this.model, DownloadState.AwaitingOnCondition);
                await Task.Run(WaitUntilConditionFinish,this.ConditionCancelToken.Token);
                this.model.ConfigViewModel.StartConditionInfo = new StartConditionInfo();
                DownloadManager.SetState(this.model, downloadState);
                DownloadManager.UpdateDownload(this.model);
            }
        }
        private IoState CheckStreamStatus()
        {
            if (this.IOCore is IOCore && this.model is DownloadViewModel)
            {
                if (!this.IOCore.isStreamOpen)
                {
                    this.IOCore.OpenFile();
                    if (this.IOCore.IoState != IoState.FileOk && !this.IOCore.isStreamOpen && this.IOCore.fileStream == null)
                    {
                        return IoState.FileNotOpen;
                    }
                }
                
                long? fileSize = this.IOCore.fileStream?.Length;
                if (fileSize == this.model.FileSize.OriginData)
                    return IoState.FileIsComplete;
                else
                {
                    return IoState.FileOk;
                }
            }
            return IoState.FileNotOpen;
        }
        private void LunchCore()
        {
            if (this.model is DownloadViewModel)
            {
                DownloadCore = CoreFactory.CreateDownloadCore(this.model.ConfigViewModel, this.model.DownloadType);
                switch (this.model.DownloadType)
                {
                    case DownloadType.HttpDownload:
                        if (DownloadCore == null)
                        {
                            AlertUser("fail to create download core. aborting...", MessageLevel.Error);
                            return;
                        }
                        DownloadCore.ResumeSupport = this.model.ResumeSupport;
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
        }
        public async void Start(bool IgnoreCondition = false)
        {
            if (!this.isWorking && !this.model.IsErrorOccurred && this.model.CurrentState != DownloadState.Verifying && this.model.CurrentState != DownloadState.Downloading && !this.model.IsCompleted)
            {
                this.IOCore.OpenFile();
                if (this.IOCore.fileStream != null && !IOEventsAssigned)
                {
                    this.IOCore.fileStream.OnMaxFile += FileStream_OnMaxFile;
                    this.IOCore.fileStream.OnStreamTermination += FileStream_OnStreamTermination;
                    IOEventsAssigned = true;
                }
                this.model.StartTime = DateTime.Now;
                if (this.model.CurrentState == DownloadState.AwaitingOnCondition)
                {
                    if (await PromptUser("This download is scheduled to run.\nDo you want to start it now?"))
                    {
                        ConditionCancelToken.Cancel();
                        this.model.ConfigViewModel.StartConditionInfo = new StartConditionInfo();
                        IgnoreCondition = true;
                        ConditionCancelToken = new CancellationTokenSource();
                        return;
                    }
                }
                if (!IgnoreCondition)
                {
                    await this.DoCondition();
                }
                this.model.StartTime = DateTime.Now;
                IoState streamStatus = this.CheckStreamStatus();
                if (streamStatus == IoState.FileOk)
                {
                    if(this.IOCore.FileSize > 0)
                        DownloadManager.SetState(this.model, DownloadState.Verifying);
                    this.LunchCore();
                }
                else if (streamStatus == IoState.FileIsComplete)
                {
                    DownloadManager.SetState(this.model, DownloadState.Completed);
                    if (await PromptUser("This download already completed.\nWant to see?"))
                    {
                        IOUtility.OpenExplorer(this.model.FileSavePath);
                    }
                    return;
                }
                else
                {
                    AlertUser($"Cannot start download\nfile is in use", MessageLevel.Error, true);
                }
            }
            else if (this.model.IsErrorOccurred)
            {
                if (this.ProcessError())
                {
                    this.model.IsErrorOccurred = false;
                    this.model.ErrorMessage = "";
                    this.Start(IgnoreCondition);
                }
            }
        }
        private async void StartHttpDownload(HttpDownloadCore core)
        {
            try
            {
                long fileSize = this.IOCore.FileSize;
                if (fileSize > 0)
                {
                    await core.GetFrom(fileSize, this.model.FileSize.OriginData);
                    DownloadManager.SetState(this.model, DownloadState.Downloading);
                    this.isWorking = true;
                }
                else
                {
                    await core.GetFrom(0, this.model.FileSize.OriginData);
                    DownloadManager.SetState(this.model, DownloadState.Downloading);
                    this.isWorking = true;
                }
                this.StartTimers();
            }
            catch (Exception ex)
            {
                this.AlertUser(ex.Message, MessageLevel.Error, true);
                DownloadManager.SetState(this.model, DownloadState.Error);
                isWorking = false;
            }
        }
        public void Stop()
        {
            if (this.model.CurrentState == DownloadState.Downloading)
            {
                if (this.model.ConfigViewModel?.StartConditionInfo.ConditionType != AutoStartConditionType.Instant)
                {
                    ConditionCancelToken.Cancel();
                    return;
                }
                DownloadCore?.Pause();
                this.IOCore.CloseFile();
                this.IOEventsAssigned = false;
                StopTimers();
                this.isWorking = false;
                DownloadManager.SetState(this.model, DownloadState.Paused);
            }
        }
        public void WaitUntilConditionFinish()
        {
            try
            {
                switch (this.model.ConfigViewModel?.StartConditionInfo.ConditionType)
                {
                    case AutoStartConditionType.Instant:
                        return;
                    case AutoStartConditionType.AbsoluteTime:
                        StartCondition = new AbsoluteTimeStartCondition(this.model)
                        {
                            StartAt = this.model.ConfigViewModel.StartConditionInfo.StartAt
                        };
                        StartCondition.WaitUntilDone(this.model, ConditionCancelToken.Token);
                        break;
                    case AutoStartConditionType.RelativeTime:
                        StartCondition = new RelativeTimeStartCondition(this.model)
                        {
                            StartIn = this.model.ConfigViewModel.StartConditionInfo.StartIn
                        };
                        StartCondition.WaitUntilDone(this.model, ConditionCancelToken.Token);
                        break;
                    case AutoStartConditionType.DownloadStateChange:
                        DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(this.model.ConfigViewModel.StartConditionInfo.DownloadId);
                        if (viewModel != null)
                        {
                            StartCondition = new DownloadCompletedCondition(this.model)
                            {
                                DownloadView = viewModel,
                                dlState = DownloadState.Completed
                            };
                            StartCondition.WaitUntilDone(this.model, ConditionCancelToken.Token);
                        }
                        else
                            return;
                        break;
                }
            }
            catch { }
            finally
            {
                this.model.ConfigViewModel.StartConditionInfo = new StartConditionInfo();
            }
        }
        #endregion
        #region VerifyingAndErrorProcessing

        private bool ProcessError()
        {
            if (this.model.IsErrorOccurred)
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
        #endregion
        #region EndDownloadTriggers


        private void ShowDialog()
        {
            GlobalSupervisor.DownloadPage?.Dispatcher.Invoke(() =>
            {
                DownloadFinishedDialog endDialog = new DownloadFinishedDialog(this.model);
                DialogManager.ShowDialog($"Finished: {this.model.DownloadName}", endDialog, DialogMode.Window);
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
        #endregion
        #region TimerCallbacks
        int counter = 0;
        private void DownloadCore_OnDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (e.Error)
            {
                if(counter < 20)
                {
                    this.Stop();
                    AlertUser("An error occurred", MessageLevel.Error, false, true);
                    this.Start(true);
                }
                else
                {
                    AlertUser("Couldn't solve error after 20 try", MessageLevel.Error);
                }
                return;
            }
            ReceivedBytes = e.ReceivedBytes;
            this.model.DownloadedSize = new UnitValue(ReceivedBytes);
            this.model.CurrentPercent = IOUtility.CalculatePercent(e.ReceivedBytes,model.FileSize.OriginData);
            try
            {
                this.IOCore.writeToFile(e.Data, e.DataLength);
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
                if ((this.model.CurrentSpeed.OriginData - LastReceivedBytes) > 100)
                {
                    speedOn = true;
                    DateTime Std = DateTime.UtcNow;
                    this.model.Ping = NetworkUtility.GetServerPing(this.model.ServerName);
                }
                LastReceivedBytes = this.model.CurrentSpeed.OriginData;
            }
            catch { }
        }

        private void SpeedTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (this.DownloadCore is IDownloadCore)
                {
                    long _speed = GlobalSupervisor.GeneralSettingsViewModel.UseBit ? (ReceivedBytes - AgoReceivedBytes) * 8 : (ReceivedBytes - AgoReceivedBytes);

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
                    this.model.ElapsedTime = ElapsedStopWatch.Elapsed;
                    this.model.CurrentSpeed = new UnitValue(currentSpeed);
                    int remainTime = (int)((this.model.FileSize.OriginData / currentSpeed) - (ReceivedBytes / currentSpeed));
                    if (speedOn)
                    {
                        DateTime Std = DateTime.UtcNow;
                        End = Std.AddSeconds(remainTime);
                    }
                    TimeSpan remainingTime = End - DateTime.UtcNow;
                    if (remainingTime >= TimeSpan.Zero)
                    {
                        this.model.RemainingTime = remainingTime;
                    }
                    this.model.DetailPage?.ChartUpdate();
                }
            }
            catch { }
        }
        #endregion
        #region Notifications
        public async void AlertUser(string message, MessageLevel level, bool notify = false, bool log = true)
        {
            string Message = $"{level}, Download name: {this.model.DownloadName}, {message}";
            if (level == MessageLevel.Error)
            {
                this.model.IsErrorOccurred = true;
                this.model.ErrorMessage = message;
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
