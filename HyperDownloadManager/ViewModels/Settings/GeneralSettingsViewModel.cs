using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Settings
{
    public class GeneralSettingsViewModel : ViewModel
    {
        public GeneralSettingsViewModel()
        {
            this.LogInMain = true;
            this.UseChart = true;
            this.NotifyOnState = true;
            this.TopMost = false;
            this.StartUp = false;
            this.RunInBack = true;
            this.SaveTemp = false;
            this.AllowDrag = true;
            this.UseBit = false;
            this.MinUnitPrefix = Unit.Mb;
            this.DefaultDownloadFolder = IOUtility.GetSystemDownloadFolder();
            this.Id = new BsonValue(Guid.NewGuid());
        }

        private Unit minUnitPrefix;
        private string defaultSaveFolder = "";
        private DownloadState draggedDownloadState;
        private int dragContainer;
        private bool logInMain, useCharts, notifyState, topMost, startUp, runBack, autoPaste, saveTemp, allowDrag, useBit;
        public bool LogInMain { get { return logInMain; } set { logInMain = value; GlobalSupervisor.MainViewModel.ShowLogInMain = value; OnPropertyChanged(); } }
        public bool UseChart { get { return useCharts; } set { useCharts = value; OnPropertyChanged(); } }
        public bool NotifyOnState { get { return notifyState; } set { notifyState = value; OnPropertyChanged(); } }
        public bool TopMost { get { return topMost; } set { topMost = value; OnPropertyChanged(); } }
        public bool StartUp { get { return startUp; } set { startUp = value; SettingSupervisor.SetStartupState(value); OnPropertyChanged(); } }
        public bool RunInBack { get { return runBack; } set { runBack = value; OnPropertyChanged(); } }
        public bool SaveTemp { get { return saveTemp; } set { saveTemp = value; OnPropertyChanged(); } }
        public bool AllowDrag { get { return allowDrag; } set { allowDrag = value; OnPropertyChanged(); } }
        public bool UseBit { get { return useBit; } set { useBit = value; OnPropertyChanged(); } }
        public Unit MinUnitPrefix { get { return minUnitPrefix; } set { minUnitPrefix = value; OnPropertyChanged(); } }
        public string? DefaultDownloadFolder
        {
            get { return defaultSaveFolder; }
            set { if (Directory.Exists(value)) { defaultSaveFolder = value; } OnPropertyChanged(); }
        }
        [BsonId]
        public BsonValue? Id { get; set; }
        
    }
}
