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
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Proxy;
using HyperDownloadManager.ViewModels.Settings;

namespace HyperDownloadManager.Views.Pages.Setting
{
    /// <summary>
    /// Interaction logic for NetworkSettingsView.xaml
    /// </summary>
    public partial class NetworkSettingsView : Page
    {
        NetworkSettingsViewModel model = new NetworkSettingsViewModel();
        public NetworkSettingsView(NetworkSettingsViewModel viewModel)
        {
            InitializeComponent();
            model = viewModel;
            this.DataContext = this.model;
        }
        private void HeaderReset_Click(object sender, RoutedEventArgs e)
        {
            this.model.Headers = NetworkUtility.ListToString(CoreFactory.NecessaryHeaders);
        }

        public async void Restore()
        {
            if (await DialogManager.ShowMessageBox("Do you want to reset the Network settings?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
            {
                SettingSupervisor.NetworkSetting = new NetworkSettingsViewModel(); 
                SettingSupervisor.SaveNetSettings();
            }
        }

        public async void Save()
        {
            if (this.proxyType.SelectedIndex == 0)
            {
                this.model.ProxyViewModel.ProxyType = ProxyType.None;
            }
            else
            {
                switch (this.proxyType.SelectedIndex)
                {
                    case 1:
                        this.model.ProxyViewModel.ProxyType = ProxyType.Http;
                        break;
                    case 2:
                        this.model.ProxyViewModel.ProxyType = ProxyType.Socks4;
                        break;
                    case 3:
                        this.model.ProxyViewModel.ProxyType = ProxyType.Socks5;
                        break;
                }
                this.model.ProxyViewModel = new ProxyViewModel()
                {
                    ProxyAddress = proxyHost.Text,
                    Port = Convert.ToUInt32(proxyPort.Text),
                    User = proxyuser.Text,
                    Pass = proxypass.Text
                };
            }
            this.model.Headers = "";
            if (this.headers.Text != "")
            {
                foreach (string line in this.headers.Text.Split('\n'))
                {
                    this.model.AddHeader(line);
                }
            }
            else
            {
                this.model.Headers = NetworkUtility.ListToString(CoreFactory.NecessaryHeaders);
            }
            this.model.EnsureSiteReturn200 = this.site200check.IsChecked ?? false;
            this.model.ResumeAfterError = this.autoresumcheck.IsChecked ?? false;
            this.model.MaxConnectionsPreServer = Convert.ToUInt32(this.Connectionspreserver.Text);
            if (Convert.ToUInt32(maxspeed.Text) != 1)
                this.model.MaxBytePreSecond = (uint)UnitConverter.ConvertToByte(Unit.Kb, Convert.ToUInt32(maxspeed.Text));
            else
                this.model.MaxBytePreSecond = 1;
            SettingSupervisor.SaveNetSettings();
            await DialogManager.ShowMessageBox("Network settings updated successfully!", MessageLevel.Info, ButtonOrder.OK, false);
        }
    }
}
