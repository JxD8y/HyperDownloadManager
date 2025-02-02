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
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Download;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace HyperDownloadManager.Views.Pages.Download
{
    /// <summary>
    /// Interaction logic for ContainerSettingsView.xaml
    /// </summary>
    public partial class ContainerSettingsView : Page
    {
        ContainerViewModel viewModel;
        private HashSet<DownloadViewModel> selectedDownloads = new HashSet<DownloadViewModel>();
        private bool _isNotifyShowing;

        public ContainerSettingsView(ContainerViewModel containerViewModel)
        {
            InitializeComponent();
            this.viewModel = containerViewModel;
            this.DataContext = containerViewModel;
            //switch (containerViewModel.ContainerAutoStartViewModel.AutoStartMode)
            //{
            //    case ContainerStartMode.Instant:
            //        StartConditionCombo.SelectedIndex = 0;
            //        break;
            //    case ContainerStartMode.AbsoluteTime:
            //        StartConditionCombo.SelectedIndex = 3;
            //        break;
            //    case ContainerStartMode.RelativeTime:
            //        StartConditionCombo.SelectedIndex = 2;
            //        break;
            //    case ContainerStartMode.Container:
            //        StartConditionCombo.SelectedIndex = 1;
            //        break;
            //}
            //switch (containerViewModel.ContainerStackMode)
            //{
            //    case ContainerStackMode.Direct:
            //        StartConditionCombo.SelectedIndex = 2;
            //        break;
            //    case ContainerStackMode.Reverse:
            //        StartConditionCombo.SelectedIndex = 1;
            //        break;
            //    case ContainerStackMode.Random:
            //        StartConditionCombo.SelectedIndex = 3;
            //        break;
            //    case ContainerStackMode.Normal:
            //        StartConditionCombo.SelectedIndex = 0;
            //        break;
            //}
            foreach (DownloadViewModel dvm in DownloadManager.DownloadViewModels)
            {
                downloadGrid.Items.Add(dvm);
            }
            foreach (ContainerViewModel cvm in ContainerManager.Containers)
            {
                conditionContainerSchedule.Items.Add(new ComboBoxItem() { Content = $"{cvm.Name}: {cvm.Id}", Tag = cvm });
            }
            downloadButtonContent.Content = $"Add Download: (0)";
        }

        private void selectFolder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FolderBrowserDialog fileBrowse = new FolderBrowserDialog();
            fileBrowse.Description = $"\"{viewModel.Name}\" path";
            fileBrowse.ShowNewFolderButton = true;
            if (fileBrowse.ShowDialog() == DialogResult.OK)
            {
                path.Text = fileBrowse.SelectedPath;
            }
        }
        #region AlertEvent
        private async void ShowNotifyMessage(string message, bool warn = false, int duration = 1000)
        {
            if (!_isNotifyShowing && alertbox != null)
            {
                _isNotifyShowing = true;
                if (warn)
                    infoicon.Visibility = Visibility.Collapsed;
                this.alertText.Text = message;
                alertbox.Visibility = Visibility.Visible;
                this.alertbox.Focus();
                alertbox.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(200)));
                await Task.Delay(duration);
                alertbox.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
                alertbox.Visibility = Visibility.Collapsed;
                infoicon.Visibility = Visibility.Visible;
                _isNotifyShowing = false;
            }
        }
        #endregion
        private async void Save_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ContainerViewModel.ValidateName(name.Text))
            {
                ShowNotifyMessage("Name is not valid", true, 1500);
                return;
            }
            if (Convert.ToInt32(maxDownload.Text) < selectedDownloads.Count)
            {
                ShowNotifyMessage("you selected more download than the Container Capacity.", true, 1500);
                return;
            }
            if (!ContainerViewModel.ValidateDescription(Description.Text))
            {
                ShowNotifyMessage("too long Description. only 120 char allowed.", true);
                return;
            }
            if (!ContainerViewModel.ValidatePath(path.Text))
            {
                ShowNotifyMessage("Path is empty or does not exist.", true);
                return;
            }
            //if (StartConditionCombo.SelectedIndex == 2)
            //{
            //    if (!ContainerAutoStartViewModel.ValidateRelativeTime(TimerDownload.Value.Value))
            //    {
            //        ShowNotifyMessage("cannot schedule for this time", true);
            //        return;
            //    }
            //}
            //else if (StartConditionCombo.SelectedIndex == 3)
            //{
            //    if (!ContainerAutoStartViewModel.ValidateAbsoluteTime(DatePicker.Value.Value))
            //    {
            //        ShowNotifyMessage("cannot schedule for this time", true);
            //        return;
            //    }
            //}
            else if (StartConditionCombo.SelectedIndex == 1)
            {
                if (conditionContainerSchedule.SelectedItem == null)
                {
                    ShowNotifyMessage("Please select a container for scheduling.", true);
                    return;
                }
            }
            viewModel.Name = name.Text;
            viewModel.Description = Description.Text;
            viewModel.MaxCapacity = Convert.ToInt32(maxDownload.Text);
            viewModel.MaxOccupied = Convert.ToInt32(maxFileSize.Text);
            viewModel.Path = path.Text;
            if (viewModel.ClearNodes())
                viewModel.AddRangeNodes(selectedDownloads);
            //switch (containerMode.SelectedIndex)
            //{
            //    case 0:
            //        viewModel.ContainerStackMode = ContainerStackMode.Normal;
            //        break;
            //    case 1:
            //        viewModel.ContainerStackMode = ContainerStackMode.Reverse;
            //        break;
            //    case 2:
            //        viewModel.ContainerStackMode = ContainerStackMode.Direct;
            //        break;
            //    case 3:
            //        viewModel.ContainerStackMode = ContainerStackMode.Random;
            //        break;
            //}
            //if (StartConditionCombo.SelectedIndex == 2)
            //{
            //    await viewModel.ContainerAutoStartViewModel.SetAutoStartMode(TimerDownload.Value.Value);
            //}
            //else if (StartConditionCombo.SelectedIndex == 3)
            //{
            //    await viewModel.ContainerAutoStartViewModel.SetAutoStartMode(DatePicker.Value.Value);

            //}
            //else if (StartConditionCombo.SelectedIndex == 1)
            //{
            //    await viewModel.ContainerAutoStartViewModel.SetAutoStartMode((conditionContainerSchedule.SelectedItem as ComboBoxItem).Tag as ContainerViewModel);
            //}
            ContainerManager.UpdateContainer(viewModel);
            ShowNotifyMessage("Container Settings Updated", false, 3000);
        }

        private void Reset_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewModel.ResetContainer();
            ShowNotifyMessage("Settings successfully reset", false, 3000);
        }

        private void addDownload_Click(object sender, RoutedEventArgs e)
        {
            if (downloadGrid.SelectedItem != null)
            {
                this.selectedDownloads.Add(downloadGrid.SelectedItem as DownloadViewModel);
            }
            downloadGrid.UnselectAll();
            downloadButtonContent.Content = $"Add Download: ({selectedDownloads.Count})";
        }
        private void deselectDownload_Click(object sender, RoutedEventArgs e)
        {
            downloadGrid.UnselectAll();
            selectedDownloads.Clear();
            downloadButtonContent.Content = $"Add Download: ({selectedDownloads.Count})";
        }
    }
}
