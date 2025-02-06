using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Proxy;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Settings
{
    public class NetworkSettingsViewModel : ViewModel
    {

        private bool ensure200, autoResume;
        private string headers = "";
        private uint bufferSize, connections, speedLimit;
        public NetworkSettingsViewModel()
        {
            this.BufferSize = 2042;
            this.MaxConnectionsPreServer = 1;
            this.EnsureSiteReturn200 = true;
            this.ResumeAfterError = true;
            this.MaxBytePreSecond = 1;
            this.Headers = NetworkUtility.ListToString(CoreFactory.NecessaryHeaders);
            this.ProxyViewModel = new ProxyViewModel();
            this.Id = new BsonValue(Guid.NewGuid());
        }
        public ProxyViewModel ProxyViewModel { get; set; } = new ProxyViewModel();
        public string Headers { get { return headers; } set { headers = value; OnPropertyChanged(); } }
        public bool EnsureSiteReturn200 { get { return ensure200; } set { ensure200 = value; OnPropertyChanged(); } }
        public bool ResumeAfterError { get { return autoResume; } set { autoResume = value; OnPropertyChanged(); } }
        public uint BufferSize { get { return bufferSize; } set { bufferSize = value; OnPropertyChanged(); } }
        public uint MaxConnectionsPreServer { get { return connections; } set { connections = value; OnPropertyChanged(); } }
        public bool CustomProxy { get { return ProxyViewModel.ContainsProxy(); } }
        public uint MaxBytePreSecond { get { return speedLimit; } set { speedLimit = value; OnPropertyChanged(); } }
        [BsonId]
        public BsonValue? Id { get; set; }

        public bool GlobalProxyExist()
        {
            return ProxyViewModel.ContainsProxy();
        }
        public void AddHeader(string header)
        {
            Headers += $"\n{header}";
        }
    }
}
