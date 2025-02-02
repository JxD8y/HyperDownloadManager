using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
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
        private bool _isNotifyShowing, _userPathSelected = false;
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
            path.Text = System.IO.Path.Combine(SettingSupervisor.GeneralSettings.DefaultDownloadFolder, ContainerManager.DefaultContainerName);
            downloadButtonContent.Content = $"Add Download: (0)";
        }
        #region ALertEvents
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
                _userPathSelected = true;
            }
        }

        private async void AddButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            if (StartConditionCombo.SelectedIndex == 2) //N: ?
            {
                //if (!ContainerAutoStartViewModel.ValidateRelativeTime(TimerDownload.Value.Value))
                //{
                //    ShowNotifyMessage("cannot schedule for this time", true);
                //    return;
                //}
            }
            else if (StartConditionCombo.SelectedIndex == 3)
            {
                //if (!ContainerAutoStartViewModel.ValidateAbsoluteTime(DatePicker.Value.Value))
                //{
                //    ShowNotifyMessage("cannot schedule for this time", true);
                //    return;
                //}
            }
            else if (StartConditionCombo.SelectedIndex == 1)
            {
                if (conditionContainerSchedule.SelectedItem == null)
                {
                    ShowNotifyMessage("Please select a container for scheduling.", true);
                    return;
                }
            }
            ContainerViewModel cvm = ContainerManager.CreateContainer(name.Text, Convert.ToInt32(maxDownload.Text), path.Text);
            cvm.AddRangeNodes(selectedDownloads);
            //switch (containerMode.SelectedIndex)
            //{
            //    case 0:
            //        cvm.ContainerStackMode = ContainerStackMode.Normal;
            //        break;
            //    case 1:
            //        cvm.ContainerStackMode = ContainerStackMode.Reverse;
            //        break;
            //    case 2:
            //        cvm.ContainerStackMode = ContainerStackMode.Direct;
            //        break;
            //    case 3:
            //        cvm.ContainerStackMode = ContainerStackMode.Random;
            //        break;
            //}
            //if (StartConditionCombo.SelectedIndex == 2)
            //{
            //    await cvm.ContainerAutoStartViewModel.SetAutoStartMode(TimerDownload.Value.Value);
            //}
            //else if (StartConditionCombo.SelectedIndex == 3)
            //{
            //    await cvm.ContainerAutoStartViewModel.SetAutoStartMode(DatePicker.Value.Value);

            //}
            //else if (StartConditionCombo.SelectedIndex == 1)
            //{
            //    await cvm.ContainerAutoStartViewModel.SetAutoStartMode((conditionContainerSchedule.SelectedItem as ComboBoxItem).Tag as ContainerViewModel);
            //}
            ContainerManager.UpdateContainer(cvm);
            ShowNotifyMessage("Container Created.", false, 3000);
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
            if (downloadGrid.SelectedItem != null)
            {
                this.selectedDownloads.Add(downloadGrid.SelectedItem as DownloadViewModel);
            }
            downloadGrid.UnselectAll();
            downloadButtonContent.Content = $"Add Download: ({selectedDownloads.Count})";
        }

        private void name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ContainerViewModel.ValidateName(name.Text))
            {
                if (!_userPathSelected)
                    path.Text = System.IO.Path.Combine(SettingSupervisor.GeneralSettings.DefaultDownloadFolder, name.Text);
            }
        }
    }
}
