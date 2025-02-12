using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using HyperDownloadManager.Dialogs.DownloadDialog;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
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
            ContainerCombo.DataContext = ContainerManager.Containers;

            if(ContainerManager.CurrentContainer == null)
            {
                if (ContainerManager.MainContainer != null)
                    ContainerManager.ChooseContainer(ContainerManager.MainContainer.Id);
                else if (ContainerManager.Containers.Count > 0)
                    ContainerManager.ChooseContainer(ContainerManager.Containers[0].Id);
                else
                {
                    throw new Exception("No container exist to load");
                }

            }

            downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
            this.DataContext = ContainerManager.CurrentContainer;
            ContainerManager.OnSelectedContainerChanged += ContainerManager_OnSelectedContainerChanged;
            DelDownload.IsEnabled = false;
            stateButton.IsEnabled = false;
            Filter_MouseLeftButtonDown(CompletedFilter, null);
        }
        private void ContainerManager_OnSelectedContainerChanged(object? sender, EventArgs e)
        {
            ContainerCombo.DataContext = ContainerManager.Containers;
            downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
            this.DataContext = ContainerManager.CurrentContainer;
        }

        private void addNew_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.ShowDialog("New Download", new NewDownloadDialog());
        }
        private void ShowDownloadDetail(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if(sender is Grid grid)
                {
                    int id = (int)(grid.Tag);
                    if (e.ClickCount == 2)
                    {
                        DownloadViewModel? downloadViewModel = DownloadManager.GetDownloadViewModel(id);
                        if (downloadViewModel != null && !downloadViewModel.IsSeparateWindowOpen)
                        {
                            GlobalSupervisor.MainWindow.mainFrame.Content = downloadViewModel.DetailPage;
                        }
                    }
                    else
                    {
                        DownloadViewModel? downloadViewModel = DownloadManager.GetDownloadViewModel(id);
                        if(downloadViewModel != null)
                        {
                            foreach (var downloadView in DownloadManager.DownloadViewModels)
                                downloadView.Selected = false;
                            downloadViewModel.Selected = true;
                            downloadViewModel.ErrorMessage = "";
                            selected_id = id;
                            stateButton.IsEnabled = true;
                            DelDownload.IsEnabled = true;
                            if (downloadViewModel.IsWorking)
                                PauseButton.Visibility = Visibility.Visible;
                            else
                                PauseButton.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Download, $"cannot show download: {ex.Message}");
            }
        }
        #region TopButtonsEvents
        #region ContainerEvents
        private void ContainerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContainerCombo.Items.Count == 1)
            {
                ContainerManager.ChooseContainer(ContainerManager.MainContainer.Id);
                downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
            }
            else
            {
                if(sender is ComboBox comboBox)
                {
                    int id = ((ContainerViewModel)(comboBox.SelectedItem)).Id;
                    ((ContainerViewModel)comboBox.SelectedItem).IsHighLighted = false;
                    ContainerManager.ChooseContainer(id);
                    downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
                }
            }

        }
        public void SelectContainer(ContainerViewModel viewModel)
        {
            this.ContainerCombo.SelectedItem = viewModel;
            ContainerManager.ChooseContainer(viewModel.Id);
        }
        private void containerAdd_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.ShowDialog("", new NewContainerDialog());
        }
        private void ContainerSettingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
                DialogManager.ShowDialog("", new ContainerSettingsView(containerViewModel));
            }
        }

        private async void containerStart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContainerViewModel? containerViewModel = ContainerManager.CurrentContainer;
            if(containerViewModel != null)
            {
                if (containerViewModel.StartConditionInfo.StartMode != ContainerStartMode.Instant)
                {
                    string info = containerViewModel.StartConditionInfo.StartMode == ContainerStartMode.AbsoluteTime ? containerViewModel.StartConditionInfo.StartIn.ToString() : containerViewModel.StartConditionInfo.StartAt.ToString();
                    var result = await DialogManager.ShowMessageBox($"this container is scheduled to start all its downloads in {info}\nDo you want to start them all now?", MessageLevel.Warning, ButtonOrder.YESNO, true);
                    if (result != null && result == MessageBoxStatus.YES)
                    {
                        containerViewModel.StartAllDownload(true);
                    }
                }
                else
                {
                    containerViewModel.StartAllDownload(false);
                }
            }
        }

        private async void containerStop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContainerViewModel? containerViewModel = ContainerManager.CurrentContainer;
            if (containerViewModel != null)
            {
                if (await DialogManager.ShowMessageBox("Do you want to Pause all download in container?\nthis may cause Data Loss\nbecause some download may not have Resume ability.", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
                {
                    containerViewModel.PauseAllDownload();
                }
            }
        }
        private async void ContainerRunAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
                if(containerViewModel.StartConditionInfo.StartMode != ContainerStartMode.Instant)
                {
                    string info = containerViewModel.StartConditionInfo.StartMode == ContainerStartMode.AbsoluteTime ? containerViewModel.StartConditionInfo.StartIn.ToString() : containerViewModel.StartConditionInfo.StartAt.ToString();
                    var result = await DialogManager.ShowMessageBox($"this container is scheduled to start all its downloads in {info}\nDo you want to start them all now?", MessageLevel.Warning, ButtonOrder.YESNO, true);
                    if(result != null && result == MessageBoxStatus.YES)
                    {
                        containerViewModel.StartAllDownload(true);
                    }
                    else
                        containerViewModel.StartAllDownload(false);
                }
            }
        }

        private async void ContainerPauseAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Do you want to Pause all download in container?\nthis may cause Data Loss\nbecause some download may not have Resume ability.", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
            {
                if (sender is MenuItem item)
                {
                    int id = (int)(item.Tag);
                    ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
                    containerViewModel.PauseAllDownload();
                }
            }
        }
        private async void ContainerRemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                ContainerViewModel containerViewModel = ContainerManager.GetContainer(id);
                if (await DialogManager.ShowMessageBox("Do you want to Remove this container?\nAll Downloads inside will remove!", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
                {
                    ContainerManager.RemoveContainer(containerViewModel, true);
                    ContainerCombo.SelectedItem = ContainerManager.MainContainer;
                }
            }
        }
        #endregion

        private async void DelDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Do you want to Remove this Download?\nFile will be removed", MessageLevel.Warning, ButtonOrder.YESNO, false) == MessageBoxStatus.YES)
            {
                DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(selected_id);
                if (viewModel != null)
                    DownloadManager.Remove(viewModel);
            }
        }
        #endregion
        #region DownloadContextEvents
        private void StartDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label)
            {
                int id = (int)(label.Tag);
                DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(id);
                if (viewModel != null)
                    DownloadManager.SetStart(viewModel);
            }
        }

        private void PauseDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label)
            {
                int id = (int)(label.Tag);
                DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(id);
                if (viewModel != null)
                    DownloadManager.SetStop(viewModel);
            }
        }

        private void downloadSettingsContext_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                DownloadViewModel? downloadViewModel = DownloadManager.GetDownloadViewModel(id);
                if(downloadViewModel != null && downloadViewModel.DownloadSettingsPage != null)
                {
                    downloadViewModel.DownloadSettingsPage = new DownloadSettingsView(downloadViewModel);
                    DialogManager.ShowDialog("Download Settings", downloadViewModel.DownloadSettingsPage, DialogMode.InApp);
                }
            }
        }

        private void startLabelContext_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(id);
                if (viewModel != null)
                    DownloadManager.SetStart(viewModel);
            }
        }

        private void pauseLabelContext_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                int id = (int)(item.Tag);
                DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(id);
                if (viewModel != null)
                    DownloadManager.SetStop(viewModel);
            }
        }

        private async void StartContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContainerViewModel? containerViewModel = ContainerManager.CurrentContainer;
            if(containerViewModel != null)
            {
                if (containerViewModel.StartConditionInfo.StartMode != ContainerStartMode.Instant)
                {
                    string info = containerViewModel.StartConditionInfo.StartMode == ContainerStartMode.AbsoluteTime ? containerViewModel.StartConditionInfo.StartIn.ToString() : containerViewModel.StartConditionInfo.StartAt.ToString();
                    var result = await DialogManager.ShowMessageBox($"this container is scheduled to start all its downloads in {info}\nDo you want to start them all now?", MessageLevel.Warning, ButtonOrder.YESNO, true);
                    if (result != null && result == MessageBoxStatus.YES)
                    {
                        containerViewModel.StartAllDownload(true);
                    }
                    else
                        containerViewModel.StartAllDownload(false);
                }
            }
        }

        private void PauseContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContainerManager.CurrentContainer?.PauseAllDownload();
        }

        private void DownloadScroller_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            Point clickPoint = e.GetPosition(scrollViewer);
            bool isItemClicked = false;
            DelDownload.IsEnabled = false;
            stateButton.IsEnabled = false;
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
                foreach (var download in downloadscontainer.Items)
                {
                    if (download is DownloadViewModel)
                    {
                        ((DownloadViewModel)download).Selected = false;
                    }
                }
            }
        }

        private void stateButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(selected_id);
            if(viewModel != null)
            {
                if (viewModel.CurrentState == DownloadState.Completed)
                {
                    stateButton.IsEnabled = false;
                    return;
                }
                if (viewModel.IsWorking)
                {
                    DownloadManager.SetStop(viewModel);
                    this.PauseButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    DownloadManager.SetStart(viewModel);
                    this.PauseButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void TopGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                GlobalSupervisor.MainWindow.DragMove();
        }

        private void searchDownload_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchBox.Text == "")
                downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
            else
            {
                downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes.Where((x) => { return x.DownloadName?.Contains(SearchBox.Text) ?? false; });
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text == "")
                downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
        }

        private void Filter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is StackPanel stack)
            {
                AllFilter.Visibility = Visibility.Collapsed;
                AllRunningFilter.Visibility = Visibility.Collapsed;
                PausedFilter.Visibility = Visibility.Collapsed;
                CompletedFilter.Visibility = Visibility.Collapsed;

                switch (stack.Name)
                {
                    case "AllFilter":
                        AllRunningFilter.Visibility = Visibility.Visible;
                        downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes.Where((v) => { return v.IsWorking; });
                        break;
                    case "AllRunningFilter":
                        PausedFilter.Visibility = Visibility.Visible;
                        downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes.Where((v) => { return !v.IsWorking && !v.IsCompleted; });
                        break;
                    case "PausedFilter":
                        CompletedFilter.Visibility = Visibility.Visible;
                        downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes.Where((v) => { return v.IsCompleted; });
                        break;
                    case "CompletedFilter":
                        AllFilter.Visibility = Visibility.Visible;
                        downloadscontainer.ItemsSource = ContainerManager.CurrentContainer?.Nodes;
                        break;
                }

            }
        }
    }
    #endregion
}

