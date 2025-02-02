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
        NetworkSettingsViewModel viewmodel = null;
        public NetworkSettingsView(NetworkSettingsViewModel model)
        {
            InitializeComponent();
            viewmodel = model;
            DataContext = model;
        }
        private void headerresetbut_Click(object sender, RoutedEventArgs e)
        {
            foreach (string line in CoreFactory.NecessaryHeaders)
            {
                this.viewmodel.AddHeader(line);
            }
        }

        public async void Restore()
        {
            if (await DialogManager.ShowMessageBox("Do you want to Restore the Network Settings?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
            {
                SettingSupervisor.LoadNetSettings();
                SettingSupervisor.SaveNetSettings();
            }
        }

        public async void Save()
        {
            if (this.proxyType.SelectedIndex == 0)
            {
                this.viewmodel.ProxyViewModel.ProxyType = ProxyType.None;
            }
            else
            {
                switch (this.proxyType.SelectedIndex)
                {
                    case 1:
                        this.viewmodel.ProxyViewModel.ProxyType = ProxyType.Http;
                        break;
                    case 2:
                        this.viewmodel.ProxyViewModel.ProxyType = ProxyType.Socks4;
                        break;
                    case 3:
                        this.viewmodel.ProxyViewModel.ProxyType = ProxyType.Socks5;
                        break;
                }
                this.viewmodel.ProxyViewModel = new ProxyViewModel()
                {
                    ProxyAddress = proxyHost.Text,
                    Port = Convert.ToUInt32(proxyPort.Text),
                    User = proxyuser.Text,
                    Pass = proxypass.Text
                };
            }
            this.viewmodel.DefaultHeaders = "";
            if (defheadertext.Text != "")
            {
                foreach (string line in defheadertext.Text.Split('\n'))
                {
                    this.viewmodel.AddHeader(line);
                }//TODO
            }
            else
            {
                foreach (string line in CoreFactory.Headers)
                {
                    this.viewmodel.AddHeader(line);
                }
            }
            this.viewmodel.EnsureSiteReturn200 = this.site200check.IsChecked.Value;
            this.viewmodel.ResumeAfterError = this.autoresumcheck.IsChecked.Value;
            this.viewmodel.MaxConnectionsPreServer = Convert.ToUInt32(this.Connectionspreserver.Text);
            if (Convert.ToUInt32(maxspeed.Text) != 1)
                this.viewmodel.MaxBytePreSecond = (uint)UnitConverter.ConvertToByte(Unit.Kb, Convert.ToUInt32(maxspeed.Text));
            else
                this.viewmodel.MaxBytePreSecond = 1;
            SettingSupervisor.SaveNetSettings();
            await DialogManager.ShowMessageBox("Network Settings Updated Successfully!", MessageLevel.Info, ButtonOrder.OK, false);
        }
    }
}
