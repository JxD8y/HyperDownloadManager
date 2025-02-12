using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Proxy;
using System.IO;
using System.Windows.Media.Animation;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download.Conditions;
using System.Windows.Forms.VisualStyles;

namespace HyperDownloadManager.Dialogs.DownloadDialog
{
    /// <summary>
    /// Interaction logic for NewDownloadDialog.xaml
    /// </summary>
    public partial class NewDownloadDialog : Page
    {
        private ConfigViewModel defaultConfig = new ConfigViewModel();
        private ContainerViewModel? downloadContainer = ContainerManager.CurrentContainer;
        private ObservableCollection<DownloadUriInfo> MultiDownloadList = new ObservableCollection<DownloadUriInfo>();
        private bool singleDownload = true, catching = false;
        private bool isNotifyShowing = false;
        private bool linkMultimode = false;
        private bool tempCreation = false;
        private string destinationFolder = "";
        private DownloadUriInfo? remoteInfo;

        public NewDownloadDialog(string Url,ContainerViewModel container):this()
        {
            this.downloadContainer = container;
            foreach(var item in ContainersCombo.Items)
            {
                if(item is ComboBoxItem boxItem)
                {
                    if((boxItem.Tag as ContainerViewModel)?.Id == container.Id)
                        ContainersCombo.SelectedItem = boxItem;
                }
            }
            this.Link.Text = Url;
            this.Link_TextChanged(this, null);
        }
        public NewDownloadDialog()
        {
            InitializeComponent();
            if (ContainerManager.Containers.Count != 0)
            {
                foreach (ContainerViewModel containerViewModel in ContainerManager.Containers)
                {
                    int index = ContainersCombo.Items.Add(new ComboBoxItem() { Content = $"{containerViewModel.Id} : {containerViewModel.Name}", Tag = containerViewModel });
                    if (ContainerManager.CurrentContainer == containerViewModel)
                    {
                        ContainersCombo.SelectedIndex = index;
                    }
                }
            }
            if (DownloadManager.Running != null && DownloadManager.Paused != null)
            {
                foreach (DownloadViewModel downloadViewModel in DownloadManager.DownloadViewModels)
                {
                    if (downloadViewModel.CurrentState != DownloadState.Completed)
                    {
                        ConditionDownload.Items.Add(new ComboBoxItem() { Content = $"{downloadViewModel.DownloadName}", Tag = downloadViewModel.Id });
                    }
                }
            }
            if (this.downloadContainer == null)
                throw new NullReferenceException("Selected container was null");

            destinationFolder = this.downloadContainer.Path;

            FilePath.Text = destinationFolder;
            multiDataDownloadGrid.ItemsSource = MultiDownloadList;

            tempCreation = GlobalSupervisor.GeneralSettingsViewModel.SaveTemp;
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
        #region LinkEvents
        private async Task<DownloadUriInfo?> GetUriInfo(Uri url)
        {
            try
            {
                catching = true;
                DownloadUriInfo? uriInfo = await HttpDownloadCore.GetUrlInfo(url,this.defaultConfig);
                if (uriInfo == null)
                {
                    ShowNotifyMessage("fail to get file info", true);
                    return null;
                }
                long ping = NetworkUtility.GetServerPing(url.Host);
                uriInfo.Ping = ping;
                uriInfo.Icon = IOUtility.GetFileIcon(uriInfo.FileName);
                catching = false;
                return uriInfo;
            }
            catch (Exception ex)
            {
                ShowNotifyMessage($"an error occurred {ex.Message}");
                return null;
            }
        }

        private void Link_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!this.linkMultimode)
                {
                    if (remoteInfo != null && !catching)
                    {
                        AddDownloadLabel_MouseLeftButtonDown(this, null);
                    }
                }
            }
        }
        private async void Link_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!linkMultimode)
            {
                string link = Link.textbox.Text;
                Uri _url;
                if (NetworkUtility.isUrl(link, out _url))
                {
                    FileIcon.Visibility = Visibility.Collapsed;
                    remoteInfo = await GetUriInfo(_url);
                    if (remoteInfo != null)
                    {
                        filename.Content = IOUtility.AdjustFileNameString(remoteInfo.FileName);
                        filesize.Content = remoteInfo.Size.DataValue.ToString("0.00");
                        fileszunit.Content = remoteInfo.Size.DataUnit.ToString();
                        pinglabel.Content = remoteInfo.Ping;
                        resumabil.Content = remoteInfo.Resumable;
                        this.FileIcon.Source = remoteInfo.Icon;
                        FileIcon.Visibility = Visibility.Visible;
                        if (!remoteInfo.Resumable)
                        {
                            ShowNotifyMessage($"this file does not have Pause ability", true, 2000);
                        }
                    }
                    else
                    {
                        AddDownloadLabel.IsEnabled = true;
                    }
                }
            }
        }
        #endregion
        #region MultiDownloadEvents
        private void DeleteSelectedDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadUriInfo info = (DownloadUriInfo)(multiDataDownloadGrid.SelectedItem);
            if (MultiDownloadList.Contains(info))
                MultiDownloadList.Remove(info);
        }
        string url = "";
        private void multiDownloadGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                DropMessage.Visibility = Visibility.Visible;
                url = (string)e.Data.GetData(DataFormats.UnicodeText);
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                url = (string)e.Data.GetData(DataFormats.Text);
                DropMessage.Visibility = Visibility.Visible;
            }
        }
        private void multiDownloadGrid_DragLeave(object sender, DragEventArgs e)
        {
            DropMessage.Visibility = Visibility.Collapsed;
        }
        private async void multiDownloadGrid_Drop(object sender, DragEventArgs e)
        {
            DropMessageText.Visibility = Visibility.Collapsed;
            multiDownloadGrid.AllowDrop = false;
            DropMessage.AllowDrop = false;
            Uri dropUri;
            if (url != "" && NetworkUtility.isUrl(url, out dropUri))
            {
                destinationFolder = FilePath.Text;
                DownloadUriInfo? remoteInfo = await GetUriInfo(dropUri);
                if (remoteInfo != null)
                    MultiDownloadList.Add(remoteInfo);

                else
                    ShowNotifyMessage("fail to add Download.", true, 1500);
            }
            multiDownloadGrid.AllowDrop = true;
            DropMessage.AllowDrop = true;
            DropMessageText.Visibility = Visibility.Visible;
            DropMessage.Visibility = Visibility.Collapsed;
            if (MultiDownloadList.Count > 0)
            {
                AddDownloadLabel.IsEnabled = true;
            }
            url = "";
        }
        #endregion

        #region UiEvents
        private void selectFolder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (fbDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FilePath.Text = fbDialog.SelectedPath;
                destinationFolder = fbDialog.SelectedPath;
                if (remoteInfo != null)
                    AddDownloadLabel.IsEnabled = true;
            }
        }
        private void ConditionDownload_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox)?.SelectedItem is ComboBoxItem item)
            {
                int? id = (int)(item).Tag;
                if (id != null && DownloadManager.GetDownloadViewModel(id.Value) is DownloadViewModel model)
                {
                    if (model.CurrentState == DownloadState.Completed)
                    {
                        ShowNotifyMessage("Selected download is completed and cannot be used as trigger", true);
                        (sender as ComboBox)?.Items.Remove((sender as ComboBox)?.SelectedIndex);
                        defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.Instant;
                        return;
                    }
                    defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.DownloadStateChange;
                    defaultConfig.StartConditionInfo.DownloadId = id.Value;
                }
            }
        }
        bool expanded = false;
        private void expandSettings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!expanded)
            {
                this.Height = 380;
                this.MaxHeight = this.Height;
                Settings.Visibility = Visibility.Visible;
                multiDownloadGrid.Height = 70;
                expanded = true;
            }
            else
            {

                if (!linkMultimode)
                {
                    multiDownloadGrid.Height = 70;
                    this.Height = 200;
                    this.MaxHeight = this.Height;
                }
                else
                {
                    multiDownloadGrid.Height = 200;
                    this.Height = 320;
                    this.MaxHeight = this.Height;
                }
                Settings.Visibility = Visibility.Collapsed;
                expanded = false;
            }

        }
        private void multiLinkMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!linkMultimode)
            {
                multiLinkMode.Visibility = Visibility.Collapsed;
                linkMultimode = true;
                ContainersCombo.Visibility = Visibility.Collapsed;
                multiDownloadGrid.Visibility = Visibility.Visible;
                this.Height = 320;
                multiDownloadGrid.Height = 200;
                this.MaxHeight = this.Height;
                Settings.Visibility = Visibility.Collapsed;
            }
            else
            {
                multiDownloadGrid.Visibility = Visibility.Visible;
                linkMultimode = false;
                ContainersCombo.Visibility = Visibility.Visible;
                multiDownloadGrid.Visibility = Visibility.Collapsed;
                this.Height = 200;
                this.MaxHeight = this.Height;
            }
        }
        #endregion

        private void AddDownloadLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!catching)
                {
                    if (linkMultimode)
                    {
                        if (string.IsNullOrEmpty(containername.Text))
                        {
                            ShowNotifyMessage("Container name cannot be empty", true);
                            return;
                        }

                        if (!ContainerManager.ContainerExist(containername.Text))
                        {
                            downloadContainer = ContainerManager.CreateContainer(containername.Text, MultiDownloadList.Count + 10);
                        }
                        else
                        {
                            ShowNotifyMessage("A container with that name already exist", true);
                            return;
                        }

                        if (!PrepareLocalSettings())
                            return;

                        foreach (DownloadUriInfo info in MultiDownloadList)
                        {
                            int? id = DownloadManager.Create(info, defaultConfig, downloadContainer);
                            if (defaultConfig.StartConditionInfo.ConditionType != AutoStartConditionType.Instant && id != null)
                            {
                                DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(id ?? 0);
                                DownloadManager.SetStart(viewModel);
                            }
                        }
                        GlobalSupervisor.DownloadPage?.SelectContainer(downloadContainer);
                    }
                    else
                    {
                        string saveDir = downloadContainer?.Path ?? "";
                        if (Directory.Exists(FilePath.Text))
                        {
                            if (FilePath.Text != downloadContainer?.Path || saveDir == "")
                            {
                                saveDir = FilePath.Text;
                                if(remoteInfo != null)
                                    remoteInfo.SaveDirectory = saveDir;
                            }
                        }
                        else
                        {
                            ShowNotifyMessage("check the selected folder.", true);
                            return;
                        }
                        if (remoteInfo != null)
                        {
                            if (!PrepareLocalSettings())
                                return;
                            if(downloadContainer != null)
                            {
                                int? id = DownloadManager.Create(remoteInfo, defaultConfig, downloadContainer);
                                if(defaultConfig.StartConditionInfo.ConditionType != AutoStartConditionType.Instant && id != null)
                                {
                                    DownloadViewModel? viewModel = DownloadManager.GetDownloadViewModel(id ?? 0);
                                    DownloadManager.SetStart(viewModel);
                                }
                            }
                            else
                            {
                                ShowNotifyMessage("container is not set", true);
                                return;
                            }
                            
                        }
                        else
                        {
                            ShowNotifyMessage("Check the Url and Selected Folder.", true);
                            return;
                        }
                    }
                }
                else
                {
                    ShowNotifyMessage("Wait until Server Send file info.", true);
                    return;
                }
            }
            catch(Exception ex)
            {
                ShowNotifyMessage(ex.Message, true);
                return;
            }
            GlobalSupervisor.MainWindow.CloseDialog(this, null);
        }

        private void ContainersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ContainersCombo.SelectedItem is ComboBoxItem item)
                this.FilePath.Text = ((ContainerViewModel)item.Tag).Path;
        }

        private void filename_Loaded(object sender, RoutedEventArgs e)
        {
            if (filename != null)
            {
                DoubleAnimation doubleAnimation = new DoubleAnimation
                {
                    From = 300,
                    To = -300,
                    Duration = new Duration(TimeSpan.FromSeconds(10)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                filename.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
            }
        }

        private bool PrepareLocalSettings()
        {
            defaultConfig = new ConfigViewModel();
            ProxyType proxyType = ViewModels.Proxy.ProxyType.None;
            if (!string.IsNullOrEmpty(proxyhost.Text))
            {
                switch (ProxyType.SelectedIndex)
                {
                    case 0:
                        proxyType = ViewModels.Proxy.ProxyType.Http;
                        break;
                    case 1:
                        proxyType = ViewModels.Proxy.ProxyType.Socks4;
                        break;
                    case 2:
                        proxyType = ViewModels.Proxy.ProxyType.Socks5;
                        break;
                }
                if (!NetworkUtility.isUrl(proxyhost.Text))
                {
                    ShowNotifyMessage("Invalid Proxy Host Address", true);
                    return false;
                }
                if (string.IsNullOrEmpty(proxyport.Text))
                {
                    ShowNotifyMessage("Proxy Port is empty", true);
                    return false;
                }
                if (!NetworkUtility.CheckProxy(new ProxyViewModel() { ProxyAddress = proxyhost.Text, ProxyType = proxyType, Port = UInt32.Parse(proxyport.Text) }))
                {
                    ShowNotifyMessage("Proxy is not Working", true);
                    return false;
                }
                defaultConfig.ProxyViewModel = new ProxyViewModel()
                {
                    ProxyAddress = proxyhost.Text,
                    ProxyType = proxyType,
                    Port = UInt32.Parse(proxyport.Text)
                };
            }
            if (GlobalSupervisor.NetworkSettingsViewModel.GlobalProxyExist() && !(defaultConfig.ProxyViewModel?.ContainsProxy() ?? false))
            {
                defaultConfig.ProxyViewModel = GlobalSupervisor.NetworkSettingsViewModel.ProxyViewModel;
            }

            if (!linkMultimode)
            {
                if (ContainersCombo.SelectedItem != null)
                {
                    if(ContainersCombo.SelectedItem is ComboBoxItem item)
                        this.downloadContainer = (ContainerViewModel)((item).Tag);
                }
                else
                {
                    ShowNotifyMessage("Container settings are invalid", true);
                    return false;
                }
            }
            
            switch (StartConditionCombo.SelectedIndex)
            {
                case 0:
                    defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.Instant;
                    break;
                case 4:
                    {
                        defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.DownloadStateChange;

                        if(ConditionDownload.SelectedItem is ComboBoxItem item)
                            defaultConfig.StartConditionInfo.DownloadId = (int)(item).Tag;
                        else
                        {
                            ShowNotifyMessage("Select a download for scheduling", true);
                            return false;
                        }
                    }
                    break;
                case 3:
                    defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.AllDownloadFinish;
                    break;
                case 1:
                    {
                        defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.RelativeTime;
                        if (TimerDownload.Value == null && TimerDownload.Value <= DateTime.Now.TimeOfDay)
                        {
                            ShowNotifyMessage("Download cannot schedule for this time", true);
                            return false;
                        }
                        defaultConfig.StartConditionInfo.StartIn = TimerDownload.Value ?? DateTime.Now.TimeOfDay;
                    }
                    break;
                case 2:
                    {
                        defaultConfig.StartConditionInfo.ConditionType = AutoStartConditionType.AbsoluteTime;
                        if (DatePicker.Value == null && DatePicker.Value <= DateTime.Now)
                        {
                            ShowNotifyMessage("Download cannot Schedule for this Date", true);
                            return false;
                        }
                        defaultConfig.StartConditionInfo.StartAt = DatePicker.Value ?? DateTime.Now;
                    }
                    break;
            }
            return true;
        }
    }
}
