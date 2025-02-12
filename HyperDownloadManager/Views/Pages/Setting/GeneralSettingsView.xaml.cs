using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.ViewModels;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Settings;

namespace HyperDownloadManager.Views.Pages.Setting
{
    /// <summary>
    /// Interaction logic for GeneralSettingsView.xaml
    /// </summary>
    public partial class GeneralSettingsView : Page
    {
        GeneralSettingsViewModel model = new GeneralSettingsViewModel();
        GeneralSettingsViewModel tempModel = new GeneralSettingsViewModel();
        int dragContainerId = 0;
        public GeneralSettingsView(GeneralSettingsViewModel viewModel)
        {
            InitializeComponent();
            this.model = viewModel;
            this.DataContext = this.model;
            UpdateComboBoxes();
        }
        private void UpdateComboBoxes()
        {
            switch (this.model.MinUnitPrefix)
            {
                case Unit.Byte:
                    MinUnit.SelectedIndex = 0;
                    break;
                case Unit.Kb:
                    MinUnit.SelectedIndex = 1;
                    break;
                case Unit.Mb:
                    MinUnit.SelectedIndex = 2;
                    break;
                case Unit.Gb:
                    MinUnit.SelectedIndex = 2;
                    break;
                case Unit.Tb:
                    MinUnit.SelectedIndex = 4;
                    break;
            }
        }
        private void MinUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MinUnit.SelectedIndex)
            {
                case 0:
                    this.tempModel.MinUnitPrefix = Unit.Byte;
                    break;
                case 1:
                    this.tempModel.MinUnitPrefix = Unit.Kb;
                    break;
                case 2:
                    this.tempModel.MinUnitPrefix = Unit.Mb;
                    break;
                case 3:
                    this.tempModel.MinUnitPrefix = Unit.Gb;
                    break;
                case 4:
                    this.tempModel.MinUnitPrefix = Unit.Tb;
                    break;
            }
        }
        private void ChangeDownloadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbDialog = new FolderBrowserDialog();
            if (fbDialog.ShowDialog() == DialogResult.OK)
            {
                this.tempModel.DefaultDownloadFolder = fbDialog.SelectedPath;
            }
        }
        #region ControlPanelFunctions
        public async void Restore()
        {
            if (await DialogManager.ShowMessageBox("Do you want to reset General settings?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
            {
                SettingSupervisor.GeneralSettings = new GeneralSettingsViewModel();
                SettingSupervisor.SaveGeneralSettings();
                UpdateComboBoxes();
            }
        }
        public async void Save()
        {
            try
            {
                this.tempModel.LogInMain = showlogcheck.IsChecked ?? false;
                this.tempModel.UseChart = usechartcheck.IsChecked ?? false;
                this.tempModel.NotifyOnState = shownotificationcheck.IsChecked ?? false;
                this.tempModel.TopMost = topmostcheck.IsChecked ?? false;
                this.tempModel.StartUp = startupcheck.IsChecked ?? false;
                this.tempModel.RunInBack = runinbackcheck.IsChecked ?? false;
                this.tempModel.UseBit = useBitMeasurement.IsChecked ?? false;
                this.tempModel.SaveTemp = tempdownloadcheck.IsChecked ?? false;
                this.tempModel.AllowDrag = allowDragCheck.IsChecked ?? false;
                this.tempModel.Id = SettingSupervisor.GeneralSettings.Id;
                SettingSupervisor.GeneralSettings = tempModel;
                SettingSupervisor.SetStartupState(this.tempModel.StartUp);
                SettingSupervisor.SaveGeneralSettings();
                await DialogManager.ShowMessageBox("Settings Updated Successfully!", MessageLevel.Info, ButtonOrder.OK, false);
            }
            catch (Exception ex)
            {
                await DialogManager.ShowMessageBox($"Cannot update settings : {ex.Message}", MessageLevel.Error, ButtonOrder.OK, true);
            }
        }
        #endregion
    }
}
