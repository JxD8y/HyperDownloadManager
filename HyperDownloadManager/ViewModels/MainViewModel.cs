using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;

namespace HyperDownloadManager.ViewModels
{
    public class MainViewModel:ViewModel
    {
        public int Downloads { get { return downloads; } private set { downloads = value; OnPropertyChanged(); } }
        public int RunningDownloads { get { return runningDownloads; } private set { runningDownloads = value; OnPropertyChanged(); } }
        public int PausedDownloads { get { return pausedDownloads; } private set { pausedDownloads = value; OnPropertyChanged(); } }
        public int AwaitingDownloads { get { return awaitingDownloads; } private set { awaitingDownloads = value; OnPropertyChanged(); } }
        public int Containers { get { return containers; } private set { containers = value; OnPropertyChanged(); } }
        public UnitValue TransmittedData { get { return transmittedData; } set { transmittedData = value; OnPropertyChanged(); } }
        public bool ShowLogInMain { get { return showLogInMain; } set { showLogInMain = value; OnPropertyChanged(); } }

        private int downloads = 0;
        private int runningDownloads = 0;
        private int containers = 0;
        private int pausedDownloads = 0;
        private int awaitingDownloads = 0;
        private bool showLogInMain = false;
        private UnitValue transmittedData;
        public MainViewModel()
        {
            DownloadManager.DownloadViewModels.CollectionChanged += DownloadViewModels_CollectionChanged;
            DownloadManager.Running.CollectionChanged += Running_CollectionChanged;
            DownloadManager.Paused.CollectionChanged += Paused_CollectionChanged;
            DownloadManager.ScheduledDownloads.CollectionChanged += ScheduledDownloads_CollectionChanged;
            ContainerManager.Containers.CollectionChanged += Containers_CollectionChanged;
        }
        public void SetTransmittedData(long transmitted)
        {
            this.TransmittedData = new UnitValue(transmitted);
        }

        private void Containers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e != null)
                this.Containers = (sender as ObservableCollection<ContainerViewModel>).Count;
        }
        private void ScheduledDownloads_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e != null)
                this.AwaitingDownloads = (sender as ObservableCollection<DownloadViewModel>).Count;
        }
        private void Paused_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e != null)
                this.PausedDownloads = (sender as ObservableCollection<DownloadViewModel>).Count;
        }
        private void Running_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e != null)
                this.RunningDownloads = (sender as ObservableCollection<DownloadViewModel>).Count;
        }
        private void DownloadViewModels_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e != null)
                this.Downloads = (sender as ObservableCollection<DownloadViewModel>).Count;
        }
    }
}
