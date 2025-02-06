using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.ViewModels.Download.Conditions;
using HyperDownloadManager.ViewModels.Proxy;
using HyperDownloadManager.ViewModels.Settings;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download
{
    public class ConfigViewModel : ViewModel
    {
        private string authUser = "", authPass = "", headers = "";
        private long maxFileSize, speedLimit;
        private uint connections;
        private FinishType finishType;
        private bool showFinalDialog;
        [BsonIgnore]
        public DownloadViewModel model { get; set; } = new DownloadViewModel();
        public StartConditionInfo StartConditionInfo = new StartConditionInfo();
        public ProxyViewModel? ProxyViewModel = new ProxyViewModel();
        public event EventHandler<List<object>>? ConfigUpdated;
        public ConfigViewModel()
        {
            if (GlobalSupervisor.NetworkSettingsViewModel is NetworkSettingsViewModel)
            {
                ProxyViewModel = GlobalSupervisor.NetworkSettingsViewModel.ProxyViewModel;
                Headers = GlobalSupervisor.NetworkSettingsViewModel.DefaultHeaders;
                connections = GlobalSupervisor.NetworkSettingsViewModel.MaxConnectionsPreServer;
            }
            else
            {
                ProxyViewModel = null;
                Headers = "";
                connections = 1;
            }
            MaxFileSize = 0;
            SpeedLimit = 0;
            CompleteType = FinishType.None;
            ShowFinalDialog = true;
        }
        public ConfigViewModel(DownloadViewModel downloadViewModel)
        {
            if (downloadViewModel == null)
                throw new ArgumentNullException("DownloadViewModel was null");
            this.model = downloadViewModel;
            if (GlobalSupervisor.NetworkSettingsViewModel is NetworkSettingsViewModel)
            {
                ProxyViewModel = GlobalSupervisor.NetworkSettingsViewModel.ProxyViewModel;
                Headers = GlobalSupervisor.NetworkSettingsViewModel.DefaultHeaders;
                connections = GlobalSupervisor.NetworkSettingsViewModel.MaxConnectionsPreServer;
            }
            else
            {
                ProxyViewModel = null;
                Headers = "";
                connections = 1;
            }
            MaxFileSize = 0;
            SpeedLimit = 0;
            CompleteType = FinishType.None;
            ShowFinalDialog = true;
            SettingSupervisor.OnSettingsChanged += SettingSupervisor_OnSettingsChanged;
        }

        private void SettingSupervisor_OnSettingsChanged(object? sender, EventArgs e)
        {
            if ((sender is NetworkSettingsViewModel) && sender != null)
            {
                if (this.model.ResumeSupport)
                {
                    this.model.Supervisor?.Stop();
                    this.model.Supervisor?.Start(true);
                }
            }
        }
        public string AuthUser { get { return authUser; } set { authUser = value; OnPropertyChanged(); } }
        public string AuthPass { get { return authPass; } set { authPass = value; OnPropertyChanged(); } }
        public string Headers { get { return headers; } set { headers = value; OnPropertyChanged(); } }
        public long MaxFileSize { get { return maxFileSize; } set { maxFileSize = value; OnPropertyChanged(); } }
        public long SpeedLimit { get { return speedLimit; } set { speedLimit = value; OnPropertyChanged(); } }
        public uint Connections { get { return connections; } set { connections = value; OnPropertyChanged(); } }
        public FinishType CompleteType { get { return finishType; } set { finishType = value; OnPropertyChanged(); } }
        public bool ShowFinalDialog { get { return showFinalDialog; } set { showFinalDialog = value; OnPropertyChanged(); } }
        #region Functions
        public bool IsValid()
        {
            return this.ValidateStartUpSettings() && connections != 0;
        }
        public bool ValidateStartUpSettings()
        {
            switch (StartConditionInfo?.ConditionType)
            {
                case AutoStartConditionType.Instant:
                    return true;
                case AutoStartConditionType.AbsoluteTime:
                    if (StartConditionInfo.StartAt > DateTime.Now)
                        return true; break;
                case AutoStartConditionType.RelativeTime:
                    return true;
                case AutoStartConditionType.DownloadStateChange:
                    if (DownloadManager.Exist(StartConditionInfo.DownloadId))
                        return true;
                    break;
                case AutoStartConditionType.AllDownloadFinish:
                    return true;
            }
            return false;
        }
        public static List<object> operator ^(ConfigViewModel old, ConfigViewModel New)//Delta Operation
        {
            List<object> DeltaList = new List<object>();
            if (old.authPass != New.authPass)
                DeltaList.Add(old.authPass);
            if (old.authUser != New.authUser)
                DeltaList.Add(old.authUser);
            if (old.Headers != New.headers)
                DeltaList.Add(old.headers);
            if (old.Connections != New.connections)
                DeltaList.Add(old.connections);
            if (old.maxFileSize != New.maxFileSize)
                DeltaList.Add(old.maxFileSize);
            if (old.speedLimit != New.speedLimit)
                DeltaList.Add(old.speedLimit);
            if (old.finishType != New.finishType)
                DeltaList.Add(old.finishType);
            if (old.showFinalDialog != New.showFinalDialog)
                DeltaList.Add(old.showFinalDialog);
            if (old.StartConditionInfo != New.StartConditionInfo)
                DeltaList.Add(old.StartConditionInfo);
            return DeltaList;
        }
        public bool UpdateSettingValue(ConfigViewModel newConfig)
        {
            if (this.model != null)
            {
                DownloadManager.UpdateDownload(this.model);
                if (this.ConfigUpdated != null)
                    ConfigUpdated(this, this ^ newConfig);
                return true;
            }
            else
                return false;
        }
        #endregion
    }
}
