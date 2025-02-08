using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.ViewModels.Proxy;

namespace HyperDownloadManager.ViewModels.Download.DownloadCore
{
    public interface IDownloadCore
    {
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;
        public event EventHandler<EventArgs> OnCompleted;
        public ConfigViewModel ConfigViewModel { get; set; }
        public bool IsWorking { get; set; }
        public bool Completed { get; set; }
        public bool ResumeSupport { get; set; }
        public Task<bool> GetFrom(long offset, long fileSize);
        public Task<byte[]?> GetBytes(int offset, int length);
        public void TunnelBy(ProxyViewModel? proxyViewModel);
        public void Pause();
    }
}
