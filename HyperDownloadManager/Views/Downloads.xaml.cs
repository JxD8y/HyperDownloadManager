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
using HyperDownloadManager.Dialogs.DowloadDialog;
using HyperDownloadManager.Dialogs.DownloadDialog;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.Views.Pages;
using HyperDownloadManager.Views.Pages.Download;

namespace HyperDownloadManager.Views
{
    /// <summary>
    /// Interaction logic for Downloads.xaml
    /// </summary>
    public partial class Downloads : Page
    {
        int selected_id = 0;
        public Downloads()
        {
            InitializeComponent();
            DelDownload.IsEnabled = false;
            ShowSettings.IsEnabled = false;
            ShowStatistic.IsEnabled = false;
            ScheduleDownload.IsEnabled = false;
            ContainerCombo.DataContext = ContainerManager.Containers;
            downloadscontainer.ItemsSource = ContainerManager.CurrentContainer.Nodes;
            this.DataContext = ContainerManager.CurrentContainer;
            ContainerManager.OnSelectedContainerChanged += ContainerManager_OnSelectedContainerChanged;
        }
        private void ContainerManager_OnSelectedContainerChanged(object? sender, EventArgs e)
        {
            ContainerCombo.DataContext = ContainerManager.Containers;
            downloadscontainer.ItemsSource = ContainerManager.CurrentContainer.Nodes;
            this.DataContext = ContainerManager.CurrentContainer;
        }

        private void addNew_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.ShowDialog("", new NewDownloadDialog());
        }
        private void ShowDownloadDetail(object sender, MouseButtonEventArgs e)
        {
            try
            {
                int id = (int)(sender as Border).Tag;
                if (e.ClickCount == 2)
                {
                    DownloadViewModel? downloadViewModel = DownloadManager.GetDownloadViewModel(id);
                    if (!downloadViewModel.IsSeparateWindowOpen)
                    {
                        GlobalSupervisor.mainwindow.mainFrame.Content = downloadViewModel.DetailPage;
                    }
                }
                else
                {
                    foreach (var ctrl in DownloadManager.DownloadViewModels) { ctrl.Selected = false; }
                    DownloadManager.GetDownloadViewModel(id).Selected = true;
                    DownloadManager.GetDownloadViewModel(id).Supervisor.LastException = null;
                    selected_id = id;
                    DelDownload.IsEnabled = true;
                    ShowSettings.IsEnabled = true;
                    ShowStatistic.IsEnabled = true;
                    ScheduleDownload.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error in Show Static", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #region TopButtonsEvents
        #region ContainerEvents
        private void ContainerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContainerCombo.Items.Count == 1)
            {
                ContainerManager.ChooseContainer(ContainerManager.MainContainer.Id);
                downloadscontainer.ItemsSource = ContainerManager.CurrentContainer.Nodes;
            }
            else
            {
                int id = (int)((ContainerViewModel)((sender as ComboBox).SelectedItem)).Id;
                ContainerViewModel downloadViewModel = ContainerManager.GetContainer(id);
                downloadViewModel.IsHighLighted = false;
                ContainerManager.ChooseContainer(id);
                downloadscontainer.ItemsSource = ContainerManager.CurrentContainer.Nodes;
            }

        }

        private void containerAdd_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.ShowDialog("", new NewContainerDialog());
        }
        private void Containersettingmenuitem_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as MenuItem).Tag;
            ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
            Dialogs.DialogManager.ShowDialog("", new ContainerSettingsView(containerViewModel));
        }
        private void ContainerRunAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as MenuItem).Tag;
            ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
            containerViewModel.StartAllDownload();
        }

        private async void ContainerPauseAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Do you want to Pause all download in container?\nthis may cause Data Loss\nbecause some download may not have Resume ability.", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
            {
                int id = (int)(sender as MenuItem).Tag;
                ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
                containerViewModel.PauseAllDownload();
            }
        }
        private void ContainerScheduleAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //TODO
        }

        private async void ContainerRemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as MenuItem).Tag;
            ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
            if (await DialogManager.ShowMessageBox("Do you want to Remove this container?\nAll Downloads inside will remove!", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
            {
                ContainerManager.RemoveContainer(containerViewModel, true);
            }
        }
        #endregion


        private void ShowStatistic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DownloadViewModel downloadViewModel = DownloadManager.GetDownloadViewModel(selected_id);
            if (!downloadViewModel.IsSeparateWindowOpen)
            {
                GlobalSupervisor.mainwindow.mainFrame.Content = downloadViewModel.DetailPage;
            }
        }

        private void ScheduleDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //TODO
        }

        private void ShowSettings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DownloadViewModel downloadViewModel = DownloadManager.GetDownloadViewModel(selected_id);
            if (!downloadViewModel.IsSeparateWindowOpen)
            {
                DialogManager.ShowDialog("Download Settings", downloadViewModel.DownloadSettingsPage, DialogMode.InApp);
            }
        }

        private async void DelDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Do you want to Remove this Download?\nFile/s will remove!", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
            {
                DownloadManager.Remove(selected_id); //rem Download Seems to have a problem
            }
        }
        #endregion
        #region DownloadContextEvents
        private void StartDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                Label label = sender as Label;
                int _id = (int)label.Tag;
                DownloadManager.SetStart(_id);
            }
        }

        private void PauseDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                Label label = sender as Label;
                int _id = (int)label.Tag;
                DownloadManager.SetStop(_id);
            }
        }

        private void scheduledownloadcontext_Click(object sender, RoutedEventArgs e)
        {
            //ToDo
        }

        private void downloadsettingscontext_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                MenuItem menuItem = sender as MenuItem;
                int _id = (int)menuItem.Tag;
                DownloadViewModel downloadViewModel = DownloadManager.GetDownloadViewModel(_id);
                DialogManager.ShowDialog("Download Settings", downloadViewModel.DownloadSettingsPage, DialogMode.InApp);
            }
        }

        private void startlabelcontext_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                MenuItem menuItem = sender as MenuItem;
                int _id = (int)menuItem.Tag;
                DownloadManager.SetStart(_id);
            }
        }

        private void pauselabelcontext_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                MenuItem menuItem = sender as MenuItem;
                int _id = (int)menuItem.Tag;
                DownloadManager.SetStop(_id);
            }
        }

        private void StartContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContainerManager.CurrentContainer.StartAllDownload();
        }

        private void PauseContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContainerManager.CurrentContainer.PauseAllDownload();
        }

        private void UnselectDownload()
        {
            foreach (var download in downloadscontainer.Items)
            {
                if (download != null)
                {
                    (download as DownloadViewModel).Selected = false;
                }
            }
        }

        private void DownloadScroller_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            Point clickPoint = e.GetPosition(scrollViewer);
            bool isItemClicked = false;
            foreach (var item in downloadscontainer.Items)
            {
                var itemContainer = downloadscontainer.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (itemContainer != null && itemContainer.IsMouseOver)
                {
                    isItemClicked = true;
                    break;
                }
            }

            if (!isItemClicked)
            {
                UnselectDownload();
            }
        }
    }
    #endregion
}

