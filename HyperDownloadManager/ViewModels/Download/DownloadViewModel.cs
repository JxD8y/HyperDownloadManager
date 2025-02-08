using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using HyperDownloadManager.Views.Pages.Download;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download
{
    public class DownloadViewModel : ViewModel
    {
        public int Id { get; set; }
        public int ContainerId { get; set; }
        #region Models
        [BsonId]
        public BsonValue? Serialized_id { get; set; }
        [BsonIgnore]
        public ContainerViewModel? Container { get { return ContainerManager.GetContainer(this.ContainerId); } }
        public DownloadSupervisor? Supervisor { get; set; }
        public ConfigViewModel? ConfigViewModel { get; set; }
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
        [BsonCtor]
        public DownloadViewModel() { }
        public DownloadViewModel(int id, ConfigViewModel configViewModel, ContainerViewModel containerViewModel)
        {
            this.Id = id;
            this.CreationDate = DateTime.Now;
            this.CurrentState = DownloadState.Paused;
            this.CurrentPercent = 0;
            this.ContainerId = containerViewModel.Id;

            this.ConfigViewModel = configViewModel;
            configViewModel.model = this;

            this.Supervisor = new DownloadSupervisor(this);

            this.DetailPage = new DownloadDetailView(this);
            this.DownloadSettingsPage = new DownloadSettingsView(this);
        }
        #endregion
        #region Properties


        [BsonIgnore]
        private bool errorOccurred = false;
        public bool ErrorOccurred { get { return errorOccurred; } set { errorOccurred = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public string LastException { get; set; } = "";

        private UnitValue speed;
        [BsonIgnore]
        public UnitValue CurrentSpeed
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
        UnitValue fileSize = new UnitValue();
        public UnitValue FileSize
        {
            get
            {
                return fileSize;
            }
            set { fileSize = value; OnPropertyChanged(); }
        }
        [BsonIgnore]
        public BitmapSource? Icon { get; set; }

        private float _CurrentPercent;

        [BsonIgnore]
        public float CurrentPercent { get { return _CurrentPercent; } set { _CurrentPercent = value; OnPropertyChanged(); } }

        private DownloadState _CurrentState;
        public DownloadState CurrentState { get { return _CurrentState; } set { _CurrentState = value; OnPropertyChanged(); } }

        private bool _Selected;
        [BsonIgnore]
        public bool Selected { get { return _Selected; } set { _Selected = value; OnPropertyChanged(); } }
        private bool _working;
        [BsonIgnore]
        public bool IsWorking { get { return _working; } set { _working = value; OnPropertyChanged(); } }
        private bool _completed;
        [BsonIgnore]
        public bool IsCompleted { get { return _completed; } set { _completed = value; OnPropertyChanged(); } }

        #region Network
        bool _resumable = false;
        public bool ResumeSupport { get { return _resumable; } set { _resumable = value; OnPropertyChanged(); } }
        string _url = "";
        public string CurrentUrl { get { return _url; } set { _url = value; OnPropertyChanged(); } }
        public DownloadType DownloadType { get; set; } = DownloadType.HttpDownload;
        [BsonIgnore]
        public bool IsErrorOccurred { get; set; }
        [BsonIgnore]
        public string? ErrorMessage { get; set; }
        [BsonIgnore]
        public long Ping { get; set; }
        #endregion

        #region FilePath
        string? _fileSavePath;
        public string? FileSavePath
        {
            get
            {
                return _fileSavePath;
            }
            set
            {
                this.CurrentSaveFileDirectory = Path.GetDirectoryName(value);
                this.CurrentFileName = Path.GetFileName(value);
                this.CurrentFileExtension = Path.GetExtension(value);
            }
        }

        string? _fileName;
        [BsonIgnore]
        public string? CurrentFileName { get { return _fileName; } set { _fileName = value; OnPropertyChanged(); } }
        string? _fileExtension;
        [BsonIgnore]
        public string? CurrentFileExtension { get { return _fileExtension; } set { _fileExtension = value; OnPropertyChanged(); } }
        string? _saveDirectory;
        [BsonIgnore]
        public string? CurrentSaveFileDirectory { get { return _saveDirectory; } set { _saveDirectory = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public bool IsTempFile { get { return CurrentFileExtension == IOUtility.TempExtension; } }
        #endregion

        [BsonIgnore]
        public string? DownloadName { get { return CurrentFileName; } }
        public DateTime CreationDate { get; set; }
        public DateTime StartTime { get; set; }
        #endregion
    }
}
