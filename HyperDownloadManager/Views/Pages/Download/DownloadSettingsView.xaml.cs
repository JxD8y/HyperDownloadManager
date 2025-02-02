using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Conditions;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Proxy;

namespace HyperDownloadManager.Views.Pages.Download
{
    /// <summary>
    /// Interaction logic for DownloadSettingsView.xaml
    /// </summary>
    public partial class DownloadSettingsView : Page
    {
        public int Id { get; set; }
        DownloadViewModel downloadViewModel = null;
        ConfigViewModel configViewModel = null;
        public DownloadSettingsView(DownloadViewModel downloadViewModel)
        {
            this.downloadViewModel = downloadViewModel;
            configViewModel = downloadViewModel.ConfigViewModel;
            Id = downloadViewModel.Id;
            DataContext = configViewModel;
            InitializeComponent();
            switch (configViewModel.CompleteType)
            {
                case FinishType.None:
                    CompletionOptionCombo.SelectedIndex = 0;
                    break;
                //case FinishType.ResumeContainer:
                //    CompletionOptionCombo.SelectedIndex = 1;
                //    break;
                case FinishType.Shutdown:
                    CompletionOptionCombo.SelectedIndex = 2;
                    break;
            }
            switch (configViewModel.StartConditionInfo.AutoType)
            {
                case AutoStartConditionType.Instant:
                    StartConditionCombo.SelectedIndex = 0;
                    break;
                case AutoStartConditionType.DownloadStateChange:
                    StartConditionCombo.SelectedIndex = 1;
                    break;
                //case Conditions.AutoStartConditionType.ContainerFinish:
                //    StartConditionCombo.SelectedIndex = 2;
                //    break;
                case AutoStartConditionType.AllDownloadFinish:
                    StartConditionCombo.SelectedIndex = 3;
                    break;
                case AutoStartConditionType.RelativeTime:
                    StartConditionCombo.SelectedIndex = 4;
                    TimerDownload.Value = configViewModel.StartConditionInfo.StartIn;
                    break;
                case AutoStartConditionType.AbsoluteTime:
                    StartConditionCombo.SelectedIndex = 5;
                    DatePicker.Value = configViewModel.StartConditionInfo.StartAt;
                    break;
            }
            switch (configViewModel.ProxyViewModel.ProxyType)
            {
                case ProxyType.None:
                    ProxyTypeCombo.SelectedIndex = 0;
                    break;
                case ProxyType.Http:
                    ProxyTypeCombo.SelectedIndex = 1;
                    break;
                case ProxyType.Socks4:
                    ProxyTypeCombo.SelectedIndex = 2;
                    break;
                case ProxyType.Socks5:
                    ProxyTypeCombo.SelectedIndex = 4;
                    break;
            }
            foreach (DownloadViewModel _downloadViewModel in DownloadManager.DownloadViewModels)
            {
                if (_downloadViewModel.Current_State != DownloadState.Completed)
                {
                    ConditionDownload.Items.Add(new ComboBoxItem()
                    {
                        Content = _downloadViewModel.DownloadName,
                        Tag = _downloadViewModel.Id,
                        IsSelected = _downloadViewModel.Id == configViewModel.StartConditionInfo.DownloadId
                    });
                }
            }
            foreach (ContainerViewModel _containerViewModel in ContainerManager.Containers)
            {
                containerSettingcombo.Items.Add(new ComboBoxItem()
                {
                    Content = _containerViewModel.Name,
                    Tag = _containerViewModel.Id,
                    IsSelected = _containerViewModel.Id == configViewModel.StartConditionInfo.ContainerId
                });
            }
        }
        private async void Savebutton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: validation process will be done by the ViewModel itself
            if (!downloadViewModel.Supervisor.IsWorking)
            {
                ConfigViewModel _newconfigViewModel = new ConfigViewModel();
                _newconfigViewModel.MaxFileSize = Convert.ToUInt32(maxfilesizebox.Text);
                _newconfigViewModel.SpeedLimit = Convert.ToUInt32(maxspeedbox.Text);
                _newconfigViewModel.Connections = Convert.ToUInt32(connectioncountbox.Text);
                switch (CompletionOptionCombo.SelectedIndex)
                {
                    case 0:
                        _newconfigViewModel.CompleteType = FinishType.None;
                        break;
                    //case 1:
                    //    _newconfigViewModel.CompleteType = FinishType.ResumeContainer;
                    //    break;
                    case 2:
                        _newconfigViewModel.CompleteType = FinishType.Shutdown;
                        break;
                }
                _newconfigViewModel.ShowFinalDialog = shownotificationcheck.IsChecked.Value;
                switch (StartConditionCombo.SelectedIndex)
                {
                    case 0:
                        _newconfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.Instant;
                        break;
                    case 1:
                        _newconfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.DownloadStateChange;
                        _newconfigViewModel.StartConditionInfo.DownloadId = (int)((ComboBoxItem)(ConditionDownload.SelectedItem)).Tag;
                        break;
                    //case 2:
                    //    _newconfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.ContainerFinish;
                    //    _newconfigViewModel.StartConditionInfo.ContainerId = (int)((ComboBoxItem)(containerSettingcombo.SelectedItem)).Tag;
                    //    break;
                    case 3:
                        _newconfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.AllDownloadFinish;
                        break;
                    case 4:
                        _newconfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.RelativeTime;
                        _newconfigViewModel.StartConditionInfo.StartIn = TimerDownload.Value.Value;
                        break;
                    case 5:
                        _newconfigViewModel.StartConditionInfo.AutoType = AutoStartConditionType.AbsoluteTime;
                        _newconfigViewModel.StartConditionInfo.StartAt = DatePicker.Value.Value;
                        break;
                }
                _newconfigViewModel.Headers = defheadertext.Text;
                _newconfigViewModel.AuthUser = authuser.Text;
                _newconfigViewModel.AuthPass = authpass.Text;
                if (!_newconfigViewModel.IsValid())
                {
                    await DialogManager.ShowMessageBox("Cannot Change Settings\nSome settings are wrong!", MessageLevel.Error, ButtonOrder.OK, true);
                }
                else
                {
                    configViewModel.UpdateSettingValue(_newconfigViewModel);
                    await DialogManager.ShowMessageBox("Setting updated successfully!", MessageLevel.Info, ButtonOrder.OK, true);
                }
            }
            else
            {
                await DialogManager.ShowMessageBox("Cannot change settings while downloading.", MessageLevel.Error, ButtonOrder.OK, true);
            }
        }

        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Are you sure to reset all download settings?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
            {
                ConfigViewModel _newconfigViewModel = new ConfigViewModel();
                configViewModel.UpdateSettingValue(_newconfigViewModel);
                await DialogManager.ShowMessageBox("Setting reset was successful.", MessageLevel.Info, ButtonOrder.OK, true);
            }
        }
    }
}
