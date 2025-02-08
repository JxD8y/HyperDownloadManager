using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Settings;

namespace HyperDownloadManager.Dialogs.DownloadDialog
{
    /// <summary>
    /// Interaction logic for NewContainerDialog.xaml
    /// </summary>
    public partial class NewContainerDialog : Page
    {
        private HashSet<DownloadViewModel> selectedDownloads = new HashSet<DownloadViewModel>();
        private bool isNotifyShowing, userPathSelected = false;
        private ContainerViewModel model = new ContainerViewModel();
        public NewContainerDialog()
        {
            InitializeComponent();
            this.name.Text = ContainerManager.DefaultContainerName;
            foreach (DownloadViewModel dvm in DownloadManager.DownloadViewModels)
            {
                downloadGrid.Items.Add(dvm);
            }
            foreach (ContainerViewModel cvm in ContainerManager.Containers)
            {
                conditionContainerSchedule.Items.Add(new ComboBoxItem() { Content = $"{cvm.Name}: {cvm.Id}", Tag = cvm });
            }
            path.Text = SettingSupervisor.GeneralSettings.DefaultDownloadFolder ?? IOUtility.GetSystemDownloadFolder() ?? "";

            downloadButtonContent.Content = $"Add Download: (0)";
        }
        #region ALertEvents
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
        private void deselectDownload_Click(object sender, RoutedEventArgs e)
        {
            downloadGrid.UnselectAll();
            selectedDownloads.Clear();
            downloadButtonContent.Content = $"Add Download: ({selectedDownloads.Count})";
        }
        private void selectFolder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FolderBrowserDialog fileBrowse = new FolderBrowserDialog();
            fileBrowse.Description = $"\"{name.Text}\" path";
            fileBrowse.ShowNewFolderButton = true;
            if (fileBrowse.ShowDialog() == DialogResult.OK)
            {
                path.Text = fileBrowse.SelectedPath;
                userPathSelected = true;
            }
        }

        private void AddButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            model = ContainerManager.CreateContainer(name.Text, Convert.ToInt32(maxDownload.Text), path.Text);
            model.AddRangeNodes(selectedDownloads);

            model.MaxOccupied = Convert.ToInt32(maxFileSize.Text);

            if (StartConditionCombo.SelectedIndex == 1)
            {
                if (model.CreationTime.TimeOfDay >= TimerDownload.Value)
                {
                    ShowNotifyMessage("cannot schedule for this time\ntry to reschedule the container in it's setting", true);
                    return;
                }
            }
            else if (StartConditionCombo.SelectedIndex == 2)
            {
                if (model.CreationTime >= DatePicker.Value)
                {
                    ShowNotifyMessage("cannot schedule for this time\ntry to reschedule the container in it's setting", true);
                    return;
                }
            }

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
            
            ShowNotifyMessage("Container created", false, 3000);
            DialogManager.Close();
        }
        bool expanded = false;

        private void expandSettings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!expanded)
            {
                this.Height = 580;
                this.MaxHeight = 580;
                expanded = true;
            }
            else
            {
                this.Height = 420;
                this.MaxHeight = 420;
                expanded = false;
            }
        }

        private void addDownload_Click(object sender, RoutedEventArgs e)
        {
            if (downloadGrid.SelectedItem is DownloadViewModel viewModel)
            {
                this.selectedDownloads.Add(viewModel);
            }
            downloadGrid.UnselectAll();
            downloadButtonContent.Content = $"Add Download: ({selectedDownloads.Count})";
        }
    }
}
