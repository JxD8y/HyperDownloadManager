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
        GeneralSettingsViewModel viewModel;
        GeneralSettingsViewModel preSaveViewModel = new GeneralSettingsViewModel();
        int dragContainerId = 0;
        public GeneralSettingsView(GeneralSettingsViewModel model)
        {
            InitializeComponent();
            viewModel = model;
            DataContext = viewModel;
            UpdateComboBoxes();
        }
        private void UpdateComboBoxes()
        {
            switch (viewModel.MinUnitPrefix)
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
            switch (viewModel.DragState)
            {
                case DownloadState.Downloading:
                    dragEvent.SelectedIndex = 0;
                    break;
                case DownloadState.Paused:
                    dragEvent.SelectedIndex = 1;
                    break;
            }
            foreach (ContainerViewModel containerViewModel in ContainerManager.Containers)
            {
                dragContainer.Items.Add(new ComboBoxItem() { Content = containerViewModel.Name, Tag = containerViewModel.Id, IsSelected = containerViewModel.Id == viewModel.DragContainer });
            }
        }
        private void MinUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MinUnit.SelectedIndex)
            {
                case 0:
                    preSaveViewModel.MinUnitPrefix = Unit.Byte;
                    break;
                case 1:
                    preSaveViewModel.MinUnitPrefix = Unit.Kb;
                    break;
                case 2:
                    preSaveViewModel.MinUnitPrefix = Unit.Mb;
                    break;
                case 3:
                    preSaveViewModel.MinUnitPrefix = Unit.Gb;
                    break;
                case 4:
                    preSaveViewModel.MinUnitPrefix = Unit.Tb;
                    break;
            }
        }

        private void dragEvent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (dragEvent.SelectedIndex)
            {
                case 0:
                    preSaveViewModel.DragState = DownloadState.Downloading;
                    break;
                case 1:
                    preSaveViewModel.DragState = DownloadState.Paused;
                    break;
            }
        }

        private void dragContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dragContainerId = (int)((ComboBoxItem)dragContainer.SelectedItem).Tag;
        }
        private void ChangeDownloadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (fbDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                preSaveViewModel.DefaultDownloadFolder = fbDialog.SelectedPath;
            }
        }
        #region ControlPanelFunctions
        public async void Restore()
        {
            if (await DialogManager.ShowMessageBox("Do you want to reset the (General Settings)?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
            {
                SettingSupervisor.SaveGeneralSettings(new GeneralSettingsViewModel());
                UpdateComboBoxes();
            }
        }
        public async void Save()
        {
            try
            {
                if (!ContainerManager.ContainerExist(this.dragContainerId))
                {
                    await DialogManager.ShowMessageBox("Selected container for drag does not exist", MessageLevel.Error, ButtonOrder.OK, true);
                }
                else
                {
                    preSaveViewModel.DragContainer = this.dragContainerId;
                }
                preSaveViewModel.LogInMain = showlogcheck.IsChecked.Value;
                preSaveViewModel.UseChart = usechartcheck.IsChecked.Value;
                preSaveViewModel.NotifyOnState = shownotificationcheck.IsChecked.Value;
                preSaveViewModel.TopMost = topmostcheck.IsChecked.Value;
                preSaveViewModel.StartUp = startupcheck.IsChecked.Value;
                preSaveViewModel.RunInBack = runinbackcheck.IsChecked.Value;
                preSaveViewModel.AutoUrlClip = autourlproccheck.IsChecked.Value;
                preSaveViewModel.MaxClipDownloadSize = int.Parse(maxClipSizeText.Text);
                preSaveViewModel.UseBit = useBitMeasurement.IsChecked.Value;
                preSaveViewModel.SaveTemp = tempdownloadcheck.IsChecked.Value;
                preSaveViewModel.AllowDrag = allowDragCheck.IsChecked.Value;
                preSaveViewModel.DragContainer = (int)((dragContainer.SelectedItem as ComboBoxItem).Tag);
                SettingSupervisor.SaveGeneralSettings(preSaveViewModel);
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
