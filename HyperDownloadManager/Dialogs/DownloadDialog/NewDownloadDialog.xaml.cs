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
using static System.Windows.Forms.LinkLabel;
using System.IO;
using System.Windows.Media.Animation;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download.Conditions;

namespace HyperDownloadManager.Dialogs.DowloadDialog
{
    /// <summary>
    /// Interaction logic for NewDownloadDialog.xaml
    /// </summary>
    public partial class NewDownloadDialog : Page
    {
        ObservableCollection<IOCore> DownloadList = new ObservableCollection<IOCore>();
        BitmapSource? bmp_src = null;
        IOCore? _ioSuper;
        ConfigViewModel _defaultConfig = new ConfigViewModel();
        ContainerViewModel? _downloadContainer = ContainerManager.CurrentContainer;
        bool _singleDownload = true, _catching = false;
        bool _isNotifyShowing = false;
        bool _LinkMultimode = false;
        bool TempCreation = false;
        string _destinationFolder = "";
        public NewDownloadDialog()
        {
            InitializeComponent();
            if (ContainerManager.Containers.Count != 0)
            {
                foreach (ContainerViewModel containerViewModel in ContainerManager.Containers)
                {
                    containerSettingcombo.Items.Add(new ComboBoxItem() { Content = $"{containerViewModel.Id} : {containerViewModel.Name}", Tag = containerViewModel.Id });
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
                    if (downloadViewModel.Current_State != DownloadState.Completed)
                    {
                        ConditionDownload.Items.Add(new ComboBoxItem() { Content = $"{downloadViewModel.DownloadName}", Tag = downloadViewModel.Id });
                    }
                }
            }
            _destinationFolder = GlobalSupervisor.GeneralSettingsViewModel.DefaultDownloadFolder;
            Filepath.Text = _destinationFolder;
            qdownloadgrid.ItemsSource = DownloadList;
            TempCreation = GlobalSupervisor.GeneralSettingsViewModel.SaveTemp;
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
        #region LinkEvents
        private async Task<IOCore?> GetUriInfo(Uri url)
        {
            try
            {
                _catching = true;
                IOCore? urlInfo = await HttpDownloadCore.GetUrlInfo(url, _defaultConfig, _destinationFolder, TempCreation);
                if (urlInfo == null)
                {
                    ShowNotifyMessage("fail to catch data.", true);
                    return null;
                }
                long ping = NetworkUtility.GetServerPing(url.Host);
                urlInfo.Ping = ping;
                urlInfo.Icon = IOUtility.GetFileIcon(urlInfo.fileName);
                _catching = false;
                return urlInfo;
            }
            catch (Exception ex)
            {
                ShowNotifyMessage($"an error occurred {ex.Message}");
            }
            return null;
        }

        private void Link_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!_LinkMultimode)
                {
                    if (IOCore.IsValidInfoCarrier(_ioSuper) && !_catching)
                    {
                        AddDownloadLabel_MouseLeftButtonDown(null, null);
                    }
                }
            }
        }
        private async void Link_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_LinkMultimode)
            {
                string link = Link.Text;
                Uri _url;
                if (NetworkUtility.isUrl(link, out _url))
                {
                    Fileextimg.Visibility = Visibility.Collapsed;
                    _ioSuper = await GetUriInfo(_url);
                    if (_ioSuper == null)
                    {
                        ShowNotifyMessage($"Cannot load url.", true, 2000);
                        return;
                    }
                    filename.Content = IOUtility.AdjustFileNameString(_ioSuper.fileName);
                    filesize.Content = _ioSuper.fileSize.DataValue.ToString("0.00");
                    fileszunit.Content = _ioSuper.fileSize.DataUnit.ToString();
                    pinglabel.Content = _ioSuper.Ping;
                    resumabil.Content = _ioSuper.Resumable;
                    this.Fileextimg.Source = _ioSuper.Icon;
                    Fileextimg.Visibility = Visibility.Visible;
                    if (!_ioSuper.Resumable)
                    {
                        ShowNotifyMessage($"this file does not have Pause ability", true, 2000);
                    }
                    if (IOCore.IsValidInfoCarrier(_ioSuper))
                    {
                        AddDownloadLabel.IsEnabled = true;

                    }
                }
            }
        }
        #endregion
        #region MultiDownloadEvents
        private void DeleteSelctedDownload_Click(object sender, RoutedEventArgs e)
        {
            IOCore id = (IOCore)qdownloadgrid.SelectedItem;
            if (DownloadList.Contains(id))
                DownloadList.Remove(id);
            if (DownloadList.Count == 0)
            {
                AddDownloadLabel.IsEnabled = false;
            }
        }
        string _url = "";
        private void qdownloadgrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                DropMessage.Visibility = Visibility.Visible;
                _url = (string)e.Data.GetData(DataFormats.UnicodeText);
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                _url = (string)e.Data.GetData(DataFormats.Text);
                DropMessage.Visibility = Visibility.Visible;
            }
        }
        private void qdownloadgrid_DragLeave(object sender, DragEventArgs e)
        {
            DropMessage.Visibility = Visibility.Collapsed;
        }
        private async void qdownloadgrid_Drop(object sender, DragEventArgs e)
        {
            DropMessageText.Visibility = Visibility.Collapsed;
            qdownloadgrid.AllowDrop = false;
            DropMessage.AllowDrop = false;
            Uri _dropUri;
            if (_url != "" && NetworkUtility.isUrl(_url, out _dropUri))
            {
                _destinationFolder = Filepath.Text;
                IOCore _ioSuperInfo = await GetUriInfo(_dropUri);
                if (_ioSuperInfo != null && IOCore.IsValidInfoCarrier(_ioSuperInfo))
                {
                    DownloadList.Add(_ioSuperInfo);
                }
                else
                {
                    ShowNotifyMessage("fail to add Download.", true, 1500);
                }
            }
            qdownloadgrid.AllowDrop = true;
            DropMessage.AllowDrop = true;
            DropMessageText.Visibility = Visibility.Visible;
            DropMessage.Visibility = Visibility.Collapsed;
            if (DownloadList.Count > 0)
            {
                AddDownloadLabel.IsEnabled = true;
            }
            _url = "";
        }
        #endregion
        #region UiEvents
        private void selectfolder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (fbDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Filepath.Text = fbDialog.SelectedPath;
                _destinationFolder = fbDialog.SelectedPath;
                //dynamic path change for single
                if (_ioSuper != null)
                {
                    _ioSuper.fileSavePath = System.IO.Path.Combine(_destinationFolder, _ioSuper.fileName);
                    if (IOCore.IsValidInfoCarrier(_ioSuper))
                    {
                        AddDownloadLabel.IsEnabled = true;
                    }
                }
                if (!_singleDownload)
                {
                    foreach (var _ioSuperInfo in DownloadList)
                    {
                        _ioSuper.fileSavePath = System.IO.Path.Combine(_destinationFolder, _ioSuper.fileName);
                    }
                }
            }
        }
        private void ConditionDownload_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int? id = (int)(((sender as ComboBox).SelectedItem) as ComboBoxItem).Tag;
            if (id != null)
            {
                if (DownloadManager.GetDownloadViewModel(id.Value).Current_State == DownloadState.Completed)
                {
                    ShowNotifyMessage("Selected download is Completed and cannot be used as trigger", true);
                    (sender as ComboBox).Items.Remove((sender as ComboBox).SelectedIndex);
                    _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.Instant;
                    return;
                }
                _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.DownloadStateChange;
                _defaultConfig.StartConditionInfo.DownloadId = id.Value;
            }
        }
        bool _expanded = false;
        private void expandSettings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_expanded)
            {
                this.Height = 380;
                this.MaxHeight = this.Height;
                Settings.Visibility = Visibility.Visible;
                qdownloadgrid.Height = 70;
                _expanded = true;
            }
            else
            {

                if (!_LinkMultimode)
                {
                    qdownloadgrid.Height = 70;
                    this.Height = 200;
                    this.MaxHeight = this.Height;
                }
                else
                {
                    qdownloadgrid.Height = 200;
                    this.Height = 320;
                    this.MaxHeight = this.Height;
                }
                Settings.Visibility = Visibility.Collapsed;
                _expanded = false;
            }

        }
        private void multilinkmode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_LinkMultimode)
            {
                multilinkmode.Visibility = Visibility.Collapsed;
                _LinkMultimode = true;
                ContainersCombo.Visibility = Visibility.Collapsed;
                multidownloadGrid.Visibility = Visibility.Visible;
                this.Height = 320;
                qdownloadgrid.Height = 200;
                this.MaxHeight = this.Height;
                Settings.Visibility = Visibility.Collapsed;
            }
            else
            {
                multilinkmode.Visibility = Visibility.Visible;
                _LinkMultimode = false;
                ContainersCombo.Visibility = Visibility.Visible;
                multidownloadGrid.Visibility = Visibility.Collapsed;
                this.Height = 200;
                this.MaxHeight = this.Height;
            }
        }

        #endregion

        private void AddDownloadLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_catching)
            {
                if (_LinkMultimode)
                {
                    if (string.IsNullOrEmpty(containername.Text))
                    {
                        ShowNotifyMessage("Container Name cannot be Empty", true);
                        return;
                    }
                    if (!ContainerManager.ContainerExist(containername.Text))
                    {
                        _downloadContainer = ContainerManager.CreateContainer(containername.Text, DownloadList.Count + 10);
                    }
                    else
                    {
                        ShowNotifyMessage("Container Name already exist", true);
                        return;
                    }
                    //ContainerStackMode stackmode = ContainerStackMode.Normal;
                    //switch (Stackmode.SelectedIndex)
                    //{
                    //    case 0:
                    //        stackmode = ContainerStackMode.Normal;
                    //        break;
                    //    case 1:
                    //        stackmode = ContainerStackMode.Direct;
                    //        break;
                    //    case 2:
                    //        stackmode = ContainerStackMode.Reverse;
                    //        break;
                    //    case 3:
                    //        stackmode = ContainerStackMode.Random;
                    //        break;
                    //}
                    //_downloadContainer.ContainerStackMode = stackmode;
                    if (!PrepareLocalSettings())
                    {
                        return;
                    }
                    foreach (IOCore ioSuperInfo in DownloadList)
                    {
                        if (!IOCore.IsValidInfoCarrier(ioSuperInfo))
                        {
                            ShowNotifyMessage("Invalid Download in List", true);
                            return;
                        }
                        DownloadManager.Create(ioSuperInfo, _defaultConfig, _downloadContainer);//In multi download mode the container path should be set to the path in newDownload Dialog
                    }
                    GlobalSupervisor.mainwindow.CloseDialog(null, null);
                }
                else
                {
                    if (Directory.Exists(Filepath.Text))
                    {
                        if (System.IO.Path.GetDirectoryName(_ioSuper.fileSavePath) != Filepath.Text)
                        {
                            _ioSuper.fileSavePath = System.IO.Path.Combine(Filepath.Text, _ioSuper.fileName);
                        }
                    }
                    else
                    {
                        ShowNotifyMessage("check the Selected Folder.", true);
                        return;
                    }
                    if (IOCore.IsValidInfoCarrier(_ioSuper))
                    {
                        if (!PrepareLocalSettings())
                        {
                            return;
                        }
                        int? id = DownloadManager.Create(_ioSuper, _defaultConfig, _downloadContainer);
                        if (id != null)
                            DialogManager.Close();
                        else
                        {
                            ShowNotifyMessage("Fail to create download.", true);
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

        private void ContainersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Filepath.Text = ((ContainersCombo.SelectedItem as ComboBoxItem).Tag as ContainerViewModel).Path;
        }

        private void filename_Loaded(object sender, RoutedEventArgs e)//C!~
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
            _defaultConfig = new ConfigViewModel();
            ProxyType proxytype = ViewModels.Proxy.ProxyType.None;
            if (!string.IsNullOrEmpty(proxyhost.Text))
            {
                switch (ProxyType.SelectedIndex)
                {
                    case 0:
                        proxytype = ViewModels.Proxy.ProxyType.Http;
                        break;
                    case 1:
                        proxytype = ViewModels.Proxy.ProxyType.Socks4;
                        break;
                    case 2:
                        proxytype = ViewModels.Proxy.ProxyType.Socks5;
                        break;
                }
                if (!NetworkUtility.isUrl(proxyhost.Text))
                {
                    ShowNotifyMessage("Invalid Proxy Host Address.", true);
                    return false;
                }
                if (string.IsNullOrEmpty(proxyport.Text))
                {
                    ShowNotifyMessage("Proxy Port is empty", true);
                    return false;
                }
                if (!NetworkUtility.CheckProxy(new ProxyViewModel() { ProxyAddress = proxyhost.Text, ProxyType = proxytype, Port = UInt32.Parse(proxyport.Text) }))
                {
                    ShowNotifyMessage("Proxy is not Working", true);
                    return false;
                }
                _defaultConfig.ProxyViewModel = new ProxyViewModel()
                {
                    ProxyAddress = proxyhost.Text,
                    ProxyType = proxytype,
                    Port = UInt32.Parse(proxyport.Text)
                };
            }
            if (GlobalSupervisor.NetworkSettingsViewModel.GlobalProxyExist() && !_defaultConfig.ProxyViewModel.ContainsProxy())
            {
                _defaultConfig.ProxyViewModel = GlobalSupervisor.NetworkSettingsViewModel.ProxyViewModel;
            }
            //container settings
            if (!_LinkMultimode)
            {
                if (ContainersCombo.SelectedItem != null)
                {
                    this._downloadContainer = (ContainerViewModel)((ContainersCombo.SelectedItem as ComboBoxItem).Tag);
                }
                else
                {
                    ShowNotifyMessage("Container Settings are Invalid", true);
                    return false;
                }
            }
            //Scheduling settings
            switch (StartConditionCombo.SelectedIndex)
            {
                case 0:
                    _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.Instant;
                    break;
                case 1:
                    {
                        _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.DownloadStateChange;
                        if (ConditionDownload.SelectedItem == null)
                        {
                            ShowNotifyMessage("Select a Download for Scheduling", true);
                            return false;
                        }
                        _defaultConfig.StartConditionInfo.DownloadId = (int)(ConditionDownload.SelectedItem as ComboBoxItem).Tag;
                    }
                    break;
                //case 2:
                //    {
                //        _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.ContainerFinish;
                //        if (containerSettingcombo.SelectedItem == null)
                //        {
                //            ShowNotifyMessage("Select a Container for Scheduling", true);
                //            return false;
                //        }
                //        _defaultConfig.StartConditionInfo.ContainerId = (int)(containerSettingcombo.SelectedItem as ComboBoxItem).Tag;
                //    }
                //    break;
                case 3:
                    _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.AllDownloadFinish;
                    break;
                case 4:
                    {
                        _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.RelativeTime;
                        if (TimerDownload.Value == null && TimerDownload.Value <= DateTime.Now.TimeOfDay)
                        {
                            ShowNotifyMessage("Download cannot Schedule for this time", true);
                            return false;
                        }
                        _defaultConfig.StartConditionInfo.StartIn = TimerDownload.Value.Value;
                    }
                    break;
                case 5:
                    {
                        _defaultConfig.StartConditionInfo.AutoType = AutoStartConditionType.AbsoluteTime;
                        if (DatePicker.Value == null && DatePicker.Value <= DateTime.Now)
                        {
                            ShowNotifyMessage("Download cannot Schedule for this Date", true);
                            return false;
                        }
                        _defaultConfig.StartConditionInfo.StartAt = DatePicker.Value.Value;
                    }
                    break;
            }
            return true;
        }
    }
}
