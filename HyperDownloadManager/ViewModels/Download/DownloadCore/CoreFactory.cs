using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.DownloadCore
{
    public static class CoreFactory
    {
        //N: contains functions that should be inside utils
        private static long LastDownloadBits = 0;
        public static List<string> Headers = new List<string>();
        public static List<string> NecessaryHeaders = new List<string>()
        {
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };
        public static event EventHandler<long>? OnDataTransmitted;
        public static IDownloadCore? CreateDownloadCore(ConfigViewModel? viewModel, DownloadType downloadType)
        {
            IDownloadCore? downloadCore = null;
            switch (downloadType)
            {
                case DownloadType.HttpDownload:
                    downloadCore = new HttpDownloadCore(viewModel);
                    ConfigureHttpDownload(viewModel, downloadCore);
                    break;
                default:
                    return null;
            }
            if (downloadCore != null)
            {
                downloadCore.OnDataReceived += DownloadCore_OnDataReceived;
            }
            return downloadCore;
        }
        private static void DownloadCore_OnDataReceived(object? sender, DataReceivedEventArgs e)
        {
            lock (new object())
            {
                LastDownloadBits += e.ReceivedBytes - e.AgoReceivedBytes;
                GlobalSupervisor.MainViewModel?.SetTransmittedData(LastDownloadBits);
            }
        }
        #region HttpCore
        private static void ConfigureHttpDownload(ConfigViewModel? configViewModel, IDownloadCore? downloadCore)
        {
            if (downloadCore == null)
                return;
            HttpDownloadCore httpDownloadCore = (HttpDownloadCore)downloadCore;
            //Performing Header Import:
            List<string> headers = new List<string>();
            List<string> configHeaders = configViewModel.Headers.Split('\n').ToList();
            configHeaders.AddRange(headers);
            if (configHeaders.Count > 0)
                RemoveDuplicateHeaders(configHeaders);
            else
                configHeaders.AddRange(NecessaryHeaders);
            httpDownloadCore.ImportHeader(configHeaders);
            //Performing Proxy Add:
            if (configViewModel.ProxyViewModel.ContainsProxy() || GlobalSupervisor.NetworkSettingsViewModel.ProxyViewModel.ContainsProxy())
                httpDownloadCore.TunnelBy(configViewModel.ProxyViewModel & GlobalSupervisor.NetworkSettingsViewModel.ProxyViewModel);
        }
        public static Dictionary<string, string>? ParseHeaders(params string[] headers)
        {
            Dictionary<string, string> httpHeaders = new Dictionary<string, string>();
            foreach (string header in headers)
            {
                if (string.IsNullOrEmpty(header))
                    return null;
                else
                {
                    string[] parsed = header.Split('\n');
                    foreach (string line in parsed)
                    {
                        string[] headerData = line.Split(':');
                        httpHeaders.Add(headerData[0], headerData[1]);
                    }
                    return httpHeaders;
                }
            }
            return null;
        }
        public static List<string> RemoveDuplicateHeaders(List<string> headers)
        {
            foreach (string header in headers)
            {
                string headerName = header.Split(": ")[0];
                if (headers.Count((hdr) => { if (hdr.Contains(headerName)) return true; else return false; }) > 1)
                {
                    headers.Remove(header);
                }
            }
            return headers;
        }
        public static IEnumerable<T> GetIEnumerable<T>(T data)
        {
            yield return data;
        }
        #endregion
    }
}
