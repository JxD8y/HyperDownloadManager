using System.Windows;
using System.Windows.Controls;
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.ViewModels.DataUnit;
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
        DownloadViewModel model = new DownloadViewModel();
        public DownloadSettingsView(DownloadViewModel viewModel)
        {
            InitializeComponent();

            this.model = viewModel;
            DataContext = this.model.ConfigViewModel;

            switch (this.model.ConfigViewModel?.CompleteType)
            {
                case FinishType.None:
                    CompletionOptionCombo.SelectedIndex = 0;
                    break;
                case FinishType.Shutdown:
                    CompletionOptionCombo.SelectedIndex = 1;
                    break;
            }

            if (this.model.ConfigViewModel?.MaxFileSize == 0)
                this.model.ConfigViewModel.MaxFileSize = (long)UnitConverter.ToGb(this.model.DownloadedSize.OriginData) + 1;

            DownloadManager.UpdateDownload(this.model);
            switch (this.model.ConfigViewModel?.StartConditionInfo.ConditionType)
            {
                case AutoStartConditionType.Instant:
                    StartConditionCombo.SelectedIndex = 0;
                    break;
                case AutoStartConditionType.DownloadStateChange:
                    StartConditionCombo.SelectedIndex = 1;
                    break;
                case AutoStartConditionType.AllDownloadFinish:
                    StartConditionCombo.SelectedIndex = 2;
                    break;
                case AutoStartConditionType.RelativeTime:
                    StartConditionCombo.SelectedIndex = 3;
                    TimerDownload.Value = this.model.ConfigViewModel.StartConditionInfo.StartIn;
                    break;
                case AutoStartConditionType.AbsoluteTime:
                    StartConditionCombo.SelectedIndex = 4;
                    DatePicker.Value = this.model.ConfigViewModel.StartConditionInfo.StartAt;
                    break;
            }

            if (this.model.ConfigViewModel?.ProxyViewModel is ProxyViewModel)
            {
                switch (this.model.ConfigViewModel.ProxyViewModel?.ProxyType)
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
            }
            else
                ProxyTypeCombo.SelectedIndex = 0;

            foreach (DownloadViewModel downloadViewModel in DownloadManager.DownloadViewModels)
            {
                if (downloadViewModel.CurrentState != DownloadState.Completed)
                {
                    ConditionDownload.Items.Add(new ComboBoxItem()
                    {
                        Content = downloadViewModel.DownloadName,
                        Tag = downloadViewModel.Id,
                        IsSelected = downloadViewModel.Id == this.model.ConfigViewModel?.StartConditionInfo.DownloadId
                    });
                }
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!model.IsWorking)
            {
                ConfigViewModel config = new ConfigViewModel();
                if(Convert.ToUInt32(maxFileSizeBox.Text) < UnitConverter.ToGb(model.DownloadedSize.OriginData))
                {
                    await DialogManager.ShowMessageBox("Max file size cannot be smaller than current downloaded size", MessageLevel.Error, ButtonOrder.OK, true);
                    return;
                }
                if (Convert.ToUInt32(maxSpeedBox.Text) < 0)
                {
                    await DialogManager.ShowMessageBox("Invalid speed limit", MessageLevel.Error, ButtonOrder.OK, true);
                    return;
                }
                if (Convert.ToUInt32(connectioncountbox.Text) != 1)
                {
                    await DialogManager.ShowMessageBox("Current version does not support multi connection", MessageLevel.Error, ButtonOrder.OK, true);
                    return;
                }
                switch (CompletionOptionCombo.SelectedIndex)
                {
                    case 0:
                        config.CompleteType = FinishType.None;
                        break;
                    case 2:
                        config.CompleteType = FinishType.Shutdown;
                        break;
                }
                config.ShowFinalDialog = shownotificationcheck.IsChecked ?? true;

                switch (StartConditionCombo.SelectedIndex)
                {
                    case 0:
                        config.StartConditionInfo.ConditionType = AutoStartConditionType.Instant;
                        break;
                    case 1:
                        config.StartConditionInfo.ConditionType = AutoStartConditionType.DownloadStateChange;
                        config.StartConditionInfo.DownloadId = (int)((ComboBoxItem)(ConditionDownload.SelectedItem)).Tag;
                        break;
                    case 2:
                        config.StartConditionInfo.ConditionType = AutoStartConditionType.AllDownloadFinish;
                        break;
                    case 3:
                        config.StartConditionInfo.ConditionType = AutoStartConditionType.RelativeTime;
                        config.StartConditionInfo.StartIn = TimerDownload.Value ?? DateTime.Now.TimeOfDay;
                        break;
                    case 4:
                        config.StartConditionInfo.ConditionType = AutoStartConditionType.AbsoluteTime;
                        config.StartConditionInfo.StartAt = DatePicker.Value ?? DateTime.Now;
                        break;
                }

                if (!config.ValidateStartUpSettings())
                {
                    await DialogManager.ShowMessageBox("Invalid startup settings detected", MessageLevel.Error, ButtonOrder.OK, true);
                    return;
                }

                config.Headers = defheadertext.Text;
                config.AuthUser = authuser.Text;
                config.AuthPass = authpass.Text;

                this.model.ConfigViewModel?.UpdateSettingValue(config);

                await DialogManager.ShowMessageBox("Setting updated successfully!", MessageLevel.Info, ButtonOrder.OK, true);
            }
            else
            {
                await DialogManager.ShowMessageBox("Cannot change settings while downloading", MessageLevel.Error, ButtonOrder.OK, true);
            }
        }

        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.model.IsWorking)
            {
                if (await DialogManager.ShowMessageBox("Are you sure to reset all download's settings?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
                {
                    ConfigViewModel config = new ConfigViewModel();
                    this.model.ConfigViewModel?.UpdateSettingValue(config);
                    await DialogManager.ShowMessageBox("Setting reset was successful.", MessageLevel.Info, ButtonOrder.OK, true);
                }
            }
            else
            {
                await DialogManager.ShowMessageBox("Cannot change settings while downloading", MessageLevel.Error, ButtonOrder.OK, true);
            }
        }
    }
}
