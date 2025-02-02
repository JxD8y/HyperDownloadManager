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
using ControlzEx.Theming;
using HyperDownloadManager.Log;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download;
using MahApps.Metro.Controls;

namespace HyperDownloadManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            //LogManager.OnLogAdd += LogManager.on; 
            GlobalSupervisor.mainwindow = this;
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
            //NetworkUtility.OnNetworkConnectivityChanged += NetworkWatchDog_OnNetworkConnectivityChanged; //N: ?
            NetworkUtility.StartNetworkConnectivityObservation();
            this.DataContext = GlobalSupervisor.MainViewModel;
            this.Topmost = GlobalSupervisor.GeneralSettingsViewModel.TopMost;
            this.AllowDrop = GlobalSupervisor.GeneralSettingsViewModel.AllowDrag;
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
                if (itm.Supervisor.DownloadCore.IsWorking && !itm.Supervisor.DownloadCore.Completed)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = $"{itm.Current_Percent.ToString("0.0")}% " + itm.Current_FileName;
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

        private void AppCloseItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("not implemented!");
        }

        private void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (sender as MenuItem);
            int id = (int)menuItem.Tag;
            DownloadViewModel downloadViewModel = DownloadManager.GetDownloadViewModel(id);
            if (!(downloadViewModel.DetailPage.DataContext as DownloadViewModel).IsSeparateWindowOpen)
            {
                DownloadWindow sp = new DownloadWindow(downloadViewModel.Current_FileName);
                sp.MainFrame.Content = downloadViewModel.DetailPage;
                downloadViewModel.DetailPage.NewWindow.Visibility = Visibility.Collapsed;
                downloadViewModel.DetailPage.Backtomain.Visibility = Visibility.Collapsed;
                (downloadViewModel.DetailPage.DataContext as DownloadViewModel).IsSeparateWindowOpen = true;
                sp.Show();
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
        private void NetworkStateIndicatorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Name == "RunNetworkStateIndicatorMenuItem")
                NetworkUtility.StartNetworkConnectivityObservation();
            else
                NetworkUtility.StopNetworkConnectivityObservation();
        }
        #endregion
        public void ShowDialog(Page Dialog)
        {
            ItemContainer.Visibility = Visibility.Visible;
            mainframelayer.Visibility = Visibility.Visible;
            //ContainerScrollbar.Visibility = Visibility.Visible;
            mainFrame.Opacity = 0.5;
            mainFrame.IsEnabled = false;
            ItemContainer.Content = Dialog;
        }
        public void CloseDialog()
        {
            CloseDialog(null, null);
        }
        public void CloseDialog(object sender, MouseButtonEventArgs e)
        {
            ItemContainer.Visibility = Visibility.Collapsed;
            mainframelayer.Visibility = Visibility.Collapsed;
            // ContainerScrollbar.Visibility = Visibility.Collapsed;
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
                        ShowDialog(GlobalSupervisor.Logpage);
                        break;
                    case "AboutMenuItem":
                        ShowDialog(GlobalSupervisor.InfoPage);
                        break;
                }
            }
        }
        #region DragDrop
        private void MetroWindow_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.AllowDrag)
            {
                if (e.Data.GetDataPresent(typeof(string)))
                {
                    e.Effects = System.Windows.DragDropEffects.Link;
                    DragNotifier.Visibility = Visibility.Visible;
                }
            }
        }

        private void MetroWindow_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.AllowDrag)
            {
                if (e.Data.GetDataPresent(typeof(string)))
                {
                    e.Effects = System.Windows.DragDropEffects.Link;
                    string Url = (string)e.Data.GetData(typeof(string));
                    DragNotifier.Visibility = Visibility.Collapsed;
                    //Show new add download page
                }
            }
        }

        private void MetroWindow_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.AllowDrag)
            {
                if (sender is MetroWindow)
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                    DragNotifier.Visibility = Visibility.Collapsed;
                }
            }
        }
        #endregion
    }
}