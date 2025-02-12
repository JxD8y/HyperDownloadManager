using System.Text;
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
using ControlzEx.Standard;
using ControlzEx.Theming;
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.DownloadDialog;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;
using MahApps.Metro.Controls;

namespace HyperDownloadManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        ContainerViewModel? containerView;
        public MainWindow()
        {
            InitializeComponent();
            LogManager.OnLogAdd += LogManager_OnLogAdd;
            GlobalSupervisor.MainWindow = this;
            ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, GlobalSupervisor.ThemeSettingsViewModel.CurrentTheme);
            this.mainFrame.Content = GlobalSupervisor.DownloadPage;
            if (!GlobalSupervisor.UnderDebug)
            {
                if (GlobalSupervisor.GeneralSettingsViewModel.RunInBack)
                {
                    HDMNotifyIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    HDMNotifyIcon.Visibility = Visibility.Collapsed;
                }
            }
            NetworkUtility.OnNetworkConnectivityChanged += NetworkWatchDog_OnNetworkConnectivityChanged;
            NetworkUtility.StartNetworkConnectivityObservation();
            this.DataContext = GlobalSupervisor.MainViewModel;
            this.Topmost = GlobalSupervisor.GeneralSettingsViewModel.TopMost;
            this.AllowDrop = GlobalSupervisor.GeneralSettingsViewModel.AllowDrag;

        }

        private void LogManager_OnLogAdd(LogViewModel lvm)
        {
            if(GlobalSupervisor.GeneralSettingsViewModel.LogInMain)
                this.Dispatcher.Invoke(() => { LastLoglabel.Content = $"{lvm.AccureTime}: {lvm.Log}"; });
        }
        #region NotifyIcon
        private void HDMNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void NotifyIconContext_Opened(object sender, RoutedEventArgs e)
        {
            HDMNotifyIcon.ContextMenu.Items.Clear();
            foreach (var itm in DownloadManager.DownloadViewModels)
            {
                if (itm.IsWorking && !itm.IsCompleted)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = $"{itm.CurrentPercent.ToString("0.0")}% " + itm.CurrentFileName;
                    menuItem.Tag = itm.Id;
                    menuItem.Click += DownloadMenuItem_Click;
                    HDMNotifyIcon.ContextMenu.Items.Add(menuItem);
                }
            }
            MenuItem closeMenuItem = new MenuItem();
            closeMenuItem.Header = "Close App";
            closeMenuItem.Click += AppCloseItem_Click; ;
            Separator sp = new Separator();
            HDMNotifyIcon.ContextMenu.Items.Add(sp);
            HDMNotifyIcon.ContextMenu.Items.Add(closeMenuItem);
        }

        private async void AppCloseItem_Click(object sender, RoutedEventArgs e)
        {
            string NonResume = "";
            bool running = false;
            foreach (DownloadViewModel viewModel in DownloadManager.DownloadViewModels)
            {
                running |= viewModel.IsWorking;
                if (!viewModel.ResumeSupport)
                    NonResume += viewModel.DownloadName + "\n";
            }

            this.Show();

            if(NonResume != "")
            {
                MessageBoxStatus? result = await DialogManager.ShowMessageBox($"these downloads cannot be resumed again: {NonResume}\nAre you sure to close the application?", Dialogs.MessageBoxDialog.MessageLevel.Warning, Dialogs.MessageBoxDialog.ButtonOrder.YESNO, true);
                if(result != null && result == MessageBoxStatus.YES)
                {
                    System.Windows.Application.Current.Shutdown(0);
                }
            }
            else if(running){
                MessageBoxStatus? result = await DialogManager.ShowMessageBox("There are some downloads running\nare you sure to close the application?", Dialogs.MessageBoxDialog.MessageLevel.Warning, Dialogs.MessageBoxDialog.ButtonOrder.YESNO, true);
                if (result != null && result == MessageBoxStatus.YES)
                {
                    System.Windows.Application.Current.Shutdown(0);
                }
            }
            else
            {
                MessageBoxStatus? result = await DialogManager.ShowMessageBox("Are you sure to close the application?", Dialogs.MessageBoxDialog.MessageLevel.Warning, Dialogs.MessageBoxDialog.ButtonOrder.YESNO, true);
                if (result != null && result == MessageBoxStatus.YES)
                {
                    System.Windows.Application.Current.Shutdown(0);
                }
            }
        }

        private void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                DownloadViewModel? downloadViewModel = DownloadManager.GetDownloadViewModel(id);
                if (downloadViewModel != null)
                {
                    if (!downloadViewModel.IsSeparateWindowOpen && downloadViewModel.DetailPage != null)
                    {
                        DownloadWindow sp = new DownloadWindow(downloadViewModel);
                        sp.MainFrame.Content = downloadViewModel.DetailPage;
                        downloadViewModel.DetailPage.NewWindow.Visibility = Visibility.Collapsed;
                        downloadViewModel.DetailPage.BackToMain.Visibility = Visibility.Collapsed;
                        downloadViewModel.IsSeparateWindowOpen = true;
                        sp.Show();
                    }
                }
            }
        }
        private void NetworkWatchDog_OnNetworkConnectivityChanged(object? sender, bool e)
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.NotifyOnState)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (e)
                        netoff.Visibility = Visibility.Collapsed;
                    else
                        netoff.Visibility = Visibility.Visible;
                });
            }
        }
        #endregion
        public void ShowDialog(Page? Dialog)
        {
            if (Dialog != null)
            {
                ItemContainer.Visibility = Visibility.Visible;
                mainframelayer.Visibility = Visibility.Visible;
                mainFrame.Opacity = 0.5;
                mainFrame.IsEnabled = false;
                ItemContainer.Content = Dialog;
            }
        }
        public void CloseDialog()
        {
            CloseDialog(this, null);
        }
        public void CloseDialog(object sender, MouseButtonEventArgs e)
        {
            ItemContainer.Visibility = Visibility.Collapsed;
            mainframelayer.Visibility = Visibility.Collapsed;
            mainFrame.Opacity = 1;
            mainFrame.IsEnabled = true;
        }
        private void InfoMenuItem_Click(object sender, RoutedEventArgs e)
        {

            if (sender is MenuItem menu)
            {
                string name = menu.Name;
                switch (name)
                {
                    case "SettingMenuItem":
                        ShowDialog(GlobalSupervisor.SettingsPage);
                        break;
                    case "LogsMenuItem":
                        ShowDialog(GlobalSupervisor.LogPage);
                        break;
                    case "AboutMenuItem":
                        ShowDialog(GlobalSupervisor.InfoPage);
                        break;
                }
            }
        }
        #region DragDrop
        bool prevIsEmpty = false;
        private void MetroWindow_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            containerView = ContainerManager.CurrentContainer;
            if (containerView != null)
            {
                prevIsEmpty = ContainerManager.CurrentContainer?.IsEmpty ?? false;
                if (GlobalSupervisor.GeneralSettingsViewModel.AllowDrag && mainFrame.IsEnabled)
                {
                    if (e.Data.GetDataPresent(typeof(string)))
                    {
                        if (ContainerManager.CurrentContainer != null)
                            ContainerManager.CurrentContainer.IsEmpty = false;
                        e.Effects = System.Windows.DragDropEffects.Link;
                        DragNotifier.Visibility = Visibility.Visible;
                        this.dragContainerName.Content = containerView.Name;
                    }
                }
            }
        }

        private void MetroWindow_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.AllowDrag && containerView != null && mainFrame.IsEnabled)
            {
                if (e.Data.GetDataPresent(typeof(string)))
                {
                    if (ContainerManager.CurrentContainer != null)
                        ContainerManager.CurrentContainer.IsEmpty = prevIsEmpty;
                    e.Effects = System.Windows.DragDropEffects.Link;
                    string Url = (string)e.Data.GetData(typeof(string));
                    DragNotifier.Visibility = Visibility.Collapsed;
                    if(NetworkUtility.isUrl(Url))
                        DialogManager.ShowDialog("New Download", new NewDownloadDialog(Url,containerView));
                }
            }
        }

        private void MetroWindow_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.AllowDrag && mainFrame.IsEnabled)
            {
                if (sender is MetroWindow)
                {
                    if (ContainerManager.CurrentContainer != null)
                        ContainerManager.CurrentContainer.IsEmpty = prevIsEmpty;
                    e.Effects = System.Windows.DragDropEffects.None;
                    DragNotifier.Visibility = Visibility.Collapsed;
                }
            }
        }
        #endregion

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!GlobalSupervisor.GeneralSettingsViewModel.RunInBack)
            {
                string NonResume = "";
                bool running = false;
                foreach (DownloadViewModel viewModel in DownloadManager.DownloadViewModels)
                {
                    running |= viewModel.IsWorking;
                    if (!viewModel.ResumeSupport)
                        NonResume += viewModel.DownloadName + "\n";
                }

                if (NonResume != "")
                {
                    MessageBoxStatus? result = await DialogManager.ShowMessageBox($"these downloads cannot be resumed again: {NonResume}\nAre you sure to close the application?", Dialogs.MessageBoxDialog.MessageLevel.Warning, Dialogs.MessageBoxDialog.ButtonOrder.YESNO, true);
                    if (result != null && result == MessageBoxStatus.YES)
                    {
                        System.Windows.Application.Current.Shutdown(0);
                    }
                }
                else if (running)
                {
                    MessageBoxStatus? result = await DialogManager.ShowMessageBox("There are some downloads running\nare you sure to close the application?", Dialogs.MessageBoxDialog.MessageLevel.Warning, Dialogs.MessageBoxDialog.ButtonOrder.YESNO, true);
                    if (result != null && result == MessageBoxStatus.YES)
                    {
                        System.Windows.Application.Current.Shutdown(0);
                    }
                }
                else
                {
                    MessageBoxStatus? result = await DialogManager.ShowMessageBox("Are you sure to close the application?", Dialogs.MessageBoxDialog.MessageLevel.Warning, Dialogs.MessageBoxDialog.ButtonOrder.YESNO, true);
                    if (result != null && result == MessageBoxStatus.YES)
                    {
                        System.Windows.Application.Current.Shutdown(0);
                    }
                }
                e.Cancel = true;
            }
            else if(this.HDMNotifyIcon.Visibility != Visibility.Collapsed)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                System.Windows.Application.Current.Shutdown(0);
            }
        }
    }
}