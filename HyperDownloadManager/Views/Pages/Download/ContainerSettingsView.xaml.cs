using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;

namespace HyperDownloadManager.Views.Pages.Download
{
    /// <summary>
    /// Interaction logic for ContainerSettingsView.xaml
    /// </summary>
    public partial class ContainerSettingsView : Page
    {
        public ContainerViewModel model;
        private HashSet<DownloadViewModel> selectedDownloads = new HashSet<DownloadViewModel>();
        private bool isNotifyShowing;

        public ContainerSettingsView(ContainerViewModel viewModel)
        {
            InitializeComponent();
            this.model = viewModel;
            this.DataContext = model;

            switch (model.StartConditionInfo.StartMode)
            {
                case ContainerStartMode.Instant:
                    StartConditionCombo.SelectedIndex = 0;
                    break;
                case ContainerStartMode.AbsoluteTime:
                    StartConditionCombo.SelectedIndex = 2;
                    this.DatePicker.Value = model.StartConditionInfo.StartIn;
                    break;
                case ContainerStartMode.RelativeTime:
                    StartConditionCombo.SelectedIndex = 1;
                    this.TimerDownload.Value = model.StartConditionInfo.StartAt;
                    break;
            }
            foreach (DownloadViewModel dvm in DownloadManager.DownloadViewModels)
            {
                if(dvm.ContainerId == this.model.Id)
                {
                    selectedDownloads.Add(dvm);
                }   
                downloadGrid.Items.Add(dvm);
            }
            downloadButtonContent.Content = $"Add Download: ({selectedDownloads.Count})";
        }

        private void selectFolder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FolderBrowserDialog fileBrowse = new FolderBrowserDialog();
            fileBrowse.Description = $"\"{model.Name}\" path";
            fileBrowse.ShowNewFolderButton = true;
            if (fileBrowse.ShowDialog() == DialogResult.OK)
            {
                path.Text = fileBrowse.SelectedPath;
            }
        }
        #region AlertEvent
        private async void ShowNotifyMessage(string message, bool warn = false, int duration = 1000)
        {
            if (!isNotifyShowing && alertbox != null)
            {
                isNotifyShowing = true;
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
                isNotifyShowing = false;
            }
        }
        #endregion
        private void Save_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ContainerViewModel.ValidateName(name.Text,model.Name))
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
            if (StartConditionCombo.SelectedIndex == 1)
            {
                if (TimerDownload.Value == TimeSpan.Zero)
                {
                    ShowNotifyMessage("cannot schedule for this time", true);
                    return;
                }
            }
            else if (StartConditionCombo.SelectedIndex == 2)
            {
                if (model.CreationTime >= DatePicker.Value)
                {
                    ShowNotifyMessage("cannot schedule for this time", true);
                    return;
                }
            }
            model.Name = name.Text;
            model.Description = Description.Text;
            model.MaxCapacity = Convert.ToInt32(maxDownload.Text);
            model.MaxOccupied = Convert.ToInt32(maxFileSize.Text);
            model.Path = path.Text;
            if (model.ClearNodes())
                model.AddRangeNodes(selectedDownloads);

            if (StartConditionCombo.SelectedIndex == 1)
            {
                model.StartConditionInfo.StartMode = ContainerStartMode.RelativeTime;
                model.StartConditionInfo.StartAt = TimerDownload.Value ?? DateTime.Now.TimeOfDay;
            }
            else if (StartConditionCombo.SelectedIndex == 2)
            {
                model.StartConditionInfo.StartMode = ContainerStartMode.AbsoluteTime;
                model.StartConditionInfo.StartIn = DatePicker.Value ?? DateTime.Now;

            }
            else
            {
                model.StartConditionInfo.StartMode = ContainerStartMode.Instant;
            }
            ContainerManager.UpdateContainer(model);

            if (model.StartConditionInfo.StartMode != ContainerStartMode.Instant)
                model.StartAllDownload();

            ShowNotifyMessage("Container settings updated", false, 3000);
        }

        private void Reset_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            model.ResetContainer();
            ShowNotifyMessage("Settings successfully reset", false, 3000);
        }

        private void addDownload_Click(object sender, RoutedEventArgs e)
        {
            if (downloadGrid.SelectedItem != null && downloadGrid.SelectedItem is  DownloadViewModel viewModel)
            {
                this.selectedDownloads.Add(viewModel);
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
