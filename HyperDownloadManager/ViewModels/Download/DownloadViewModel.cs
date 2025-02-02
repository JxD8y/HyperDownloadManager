using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using HyperDownloadManager.Views.Pages.Download;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download
{
    public class DownloadViewModel : ViewModel //N: Transfer all IOCore's props that needed to be serialized to this!
    {
        public int Id { get; set; }
        public int ContainerId { get; set; }
        #region Models
        [BsonId]
        public BsonValue? Serialized_id { get; set; }
        [BsonIgnore]
        public ContainerViewModel? Container { get { return ContainerManager.GetContainer(this.ContainerId); } }
        public IOCore IOCore { get; set; } = new IOCore();
        public DownloadSupervisor? Supervisor { get; set; }
        public ConfigViewModel ConfigViewModel { get; set; } = new ConfigViewModel();
        #region Pages
        [BsonIgnore]
        public DownloadDetailView? DetailPage { get; set; }
        [BsonIgnore]
        public DownloadSettingsView? DownloadSettingsPage { get; set; }
        [BsonIgnore]
        public DownloadWindow? DownloadWindow { get; set; }
        [BsonIgnore]
        public bool IsSeparateWindowOpen { get; set; }
        #endregion
        public DownloadViewModel(int id, IOCore ioCore, ConfigViewModel configViewModel, ContainerViewModel containerViewModel)
        {
            Id = id;
            IOCore = ioCore;
            IOCore.DownloadViewModel = this;
            Current_State = DownloadState.Paused;
            Current_Percent = 0;
            this.ConfigViewModel = configViewModel;
            configViewModel.DownloadViewModel = this;
            Supervisor = new DownloadSupervisor(this);
            DetailPage = new DownloadDetailView(id);
            DownloadSettingsPage = new DownloadSettingsView(this);
            DetailPage.SetdataContext(this);
            this.CreationDate = DateTime.Now;
            DetailPage.SetdataContext(this);
            this.ContainerId = containerViewModel.Id;
        }
        public DownloadViewModel()
        {
            IOCore = new IOCore();
        }

        #endregion
        #region Properties
        private UnitValue speed;
        [BsonIgnore]
        public UnitValue Current_Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
                OnPropertyChanged();
            }
        }
        TimeSpan remain;
        [BsonIgnore]
        public TimeSpan RemainingTime
        {
            get
            {
                return remain;
            }
            set
            {
                remain = value;
                OnPropertyChanged();
            }
        }
        TimeSpan elapsed;
        [BsonIgnore]
        public TimeSpan ElapsedTime
        {
            get
            {
                return elapsed;
            }
            set
            {
                elapsed = value;
                OnPropertyChanged();
            }
        }
        UnitValue downloadedSize;
        [BsonIgnore]
        public UnitValue DownloadedSize
        {
            get
            {
                return downloadedSize;
            }
            set
            {
                downloadedSize = value;
                OnPropertyChanged();
            }
        }
        public UnitValue? FileSize
        {
            get
            {
                return IOCore.fileSize;
            }
        }
        [BsonIgnore]
        public BitmapSource? Icon { get { return IOCore?.Icon; } set { IOCore.Icon = value; } }

        private float _Current_Percent;

        [BsonIgnore]
        public float Current_Percent { get { return _Current_Percent; } set { _Current_Percent = value; OnPropertyChanged(); } }

        private DownloadState _Current_State;
        public DownloadState Current_State { get { return _Current_State; } set { _Current_State = value; OnPropertyChanged(); } }

        private bool _Selected;
        [BsonIgnore]
        public bool Selected { get { return _Selected; } set { _Selected = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public bool ResumeSupport { get { return IOCore.Resumable; } set { IOCore.Resumable = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public string Current_Url { get { return IOCore.Url.OriginalString; } }
        [BsonIgnore]
        public string Current_Server { get { return IOCore.Url.Host; } }
        [BsonIgnore]
        public string Current_FileName { get { return IOCore.fileName; } }
        [BsonIgnore]
        public string DownloadName { get { return Current_FileName; } }
        public DateTime CreationDate { get; set; }
        public DateTime StartTime { get; set; }
        #endregion
    }
}
