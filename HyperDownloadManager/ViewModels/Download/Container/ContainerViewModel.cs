using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using System.Timers;

namespace HyperDownloadManager.ViewModels.Download.Container
{
    public class ContainerViewModel : ViewModel //N: container conditions should be loaded into this file
    {
        const double CheckRemainingInterval = 2000;
        #region ContainerSettings
        [BsonIgnore]
        private string _name;
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged(); } }
        [BsonIgnore]
        private string _path;
        public string Path { get { return _path; } set { _path = value; OnPropertyChanged(); } }
        [BsonIgnore]
        private string _description;
        public string Description { get { return _description; } set { _description = value; OnPropertyChanged(); } }
        [BsonIgnore]
        private int _maxcapacity;
        public int MaxCapacity { get { return _maxcapacity; } set { _maxcapacity = value; OnPropertyChanged(); } }
        [BsonIgnore]
        private int _maxoccupied;
        public int MaxOccupied { get { return _maxoccupied; } set { _maxoccupied = value; OnPropertyChanged(); } }
        #endregion
        #region MainProperties
        public int Id { get; set; }
        public bool IsMain { get; set; }
        [BsonId]
        public BsonValue Serialized_id { get; set; }
        [BsonIgnore]
        private bool _isHighlighted;
        [BsonIgnore]
        public bool IsHighLighted { get { return _isHighlighted; } set { _isHighlighted = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public DateTime CreationTime { get; set; }
        private bool _isCurrentContainer;
        public bool IsCurrentContainer { get { return _isCurrentContainer; } set { _isCurrentContainer = value; OnPropertyChanged(); } }
        [BsonIgnore]

        #endregion
        #region Constructors
        public ContainerViewModel(string name, int maxcapacity, int id, BsonValue serialized_id)
        {
            Id = id;
            Name = name;
            MaxCapacity = maxcapacity;
            Nodes = new ObservableCollection<DownloadViewModel>();
            Serialized_id = serialized_id;
            remainingTimer.Elapsed += RemainingTimer_Elapsed;
            remainingTimer.Start();

        }
        public ContainerViewModel()
        {
            Nodes = new ObservableCollection<DownloadViewModel>();
            remainingTimer.Elapsed += RemainingTimer_Elapsed;
            remainingTimer.Start();
        }
        #endregion
        #region DownloadManaging
        private System.Timers.Timer remainingTimer = new System.Timers.Timer(CheckRemainingInterval);
        private TimeSpan _completeTime;
        [BsonIgnore]
        public TimeSpan CompleteTime { get { return _completeTime; } set { _completeTime = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public ObservableCollection<DownloadViewModel> Nodes { get; set; } = new ObservableCollection<DownloadViewModel>();
        public int Actives { get { return Nodes.Count((d) => { return d.Current_State == DownloadState.Downloading; }); } }
        public int Paused { get { return Nodes.Count((d) => { return d.Current_State == DownloadState.Paused; }); } }
        public bool Completed { get { return Nodes.Count((d) => { return d.Current_State == DownloadState.Completed; }) == Nodes.Count; } }
        #endregion
        #region ContainerNodesOperation
        public DownloadViewModel GetNode(int id)
        {
            return Nodes[id];
        }
        public bool AddDownload(DownloadViewModel node, bool Move = false)
        {
            if (node.ContainerId != -1 && Move)
                ContainerManager.MoveDownload(node, this);
            if (!Nodes.Contains(node) && Nodes.Count != MaxCapacity && node.Serialized_id != null)
            {
                Nodes.Add(node);
                node.ContainerId = Id;
                ContainerManager.UpdateContainer(this);
                DownloadManager.UpdateDownload(node);
                return true;
            }
            return false;
        }
        public bool ClearNodes()
        {
            foreach (DownloadViewModel dvm in Nodes)
            {
                dvm.ContainerId = -1;
                DownloadManager.UpdateDownload(dvm);
                ContainerManager.UpdateContainer(this);
            }
            Nodes.Clear();
            if (Nodes.Count == 0)
                return true;
            return false;
        }
        public bool DeleteDownload(DownloadViewModel node)
        {
            if (Nodes.Contains(node))
            {
                Nodes.Remove(node);
                node.ContainerId = -1;
                DownloadManager.UpdateDownload(node);
                ContainerManager.UpdateContainer(this);
                return true;
            }
            return false;
        }
        public void AddRangeNodes(IEnumerable<DownloadViewModel> nodes)
        {
            foreach (var node in nodes)
            {
                AddDownload(node, true);
            }
        }
        #endregion
        #region ContainerDownloadOperations
        public void StartAllDownload()
        {
            foreach (DownloadViewModel downloadViewModel in Nodes)
            {
                if (downloadViewModel.Current_State != DownloadState.Completed)
                {
                    DownloadManager.SetStart(downloadViewModel.Id);
                }
            }
        }
        public void PauseAllDownload()
        {
            foreach (DownloadViewModel downloadViewModel in Nodes)
            {
                if (downloadViewModel.Current_State != DownloadState.Completed)
                {
                    if (downloadViewModel.ResumeSupport)
                    {
                        DownloadManager.SetStop(downloadViewModel.Id);
                    }
                }
            }
        }
        public void HighLightDownload(int downloadid)
        {
            IsHighLighted = true;
            DownloadViewModel downloadViewModel = GetNode(downloadid);
        }
        private void RemainingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (Nodes.Count != 0)
            {
                TimeSpan remainTime = TimeSpan.FromSeconds(1);
                foreach (DownloadViewModel dvm in Nodes)
                {
                    if (dvm.Current_State == DownloadState.Downloading)
                    {
                        if (remainTime < dvm.RemainingTime)
                        {
                            remainTime = dvm.RemainingTime;
                        }
                        CompleteTime = remainTime;
                    }
                }
            }
        }
        public void ResetContainer()
        {
            this.MaxOccupied = -1;
            this.MaxCapacity = ContainerManager.ContainerMaxCapacity;
            this.ClearNodes();
            ContainerManager.UpdateContainer(this);
        }
        #endregion
        #region Validators
        public static bool ValidateName(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Contains("\"") && !name.Contains("'") && !name.Contains("\\");
        }
        public static bool ValidateDescription(string description)
        {
            return description.Length <= 120;
        }
        public static bool ValidatePath(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }
        #endregion
    }
}
