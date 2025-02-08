using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using HyperDownloadManager.ViewModels.Proxy;

namespace HyperDownloadManager.ViewModels.Download.DownloadCore
{
    internal class HttpDownloadCore: IDownloadCore
    {
        #region Properties
        private HttpClient? httpClient;
        private long receivedBytes = 0;
        private long agoReceivedBytes = 0;
        private bool corePrepared = false;
        private bool ensure200 = false;
        private byte[] buffer = new byte[2042];
        private HttpMessageHandler? httpHandler;
        private List<string>? workingHeaders;
        public ConfigViewModel ConfigViewModel { get; set; } = new ConfigViewModel();
        public bool IsWorking { get; set; }
        public bool Completed { get; set; }
        public bool ResumeSupport { get; set; }
        public event EventHandler<DataReceivedEventArgs>? OnDataReceived;
        public event EventHandler<EventArgs>? OnCompleted;
        #endregion
        private static HttpClient? infoClient = null;
        public static async Task<DownloadUriInfo?> GetUrlInfo(Uri uri, ConfigViewModel? configViewModel)
        {
            if (uri == null) 
                throw new ArgumentNullException("uri was null");

            if (configViewModel == null)
                configViewModel = new ConfigViewModel();

            infoClient = new HttpClient();
            bool acceptRange = false;

            foreach (string Header in configViewModel.Headers.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(Header))
                {
                    string[] parts = Header.Split(':');
                    infoClient.DefaultRequestHeaders.Add(parts[0], parts[1]);
                }
            }

            infoClient.BaseAddress = uri;
            HttpResponseMessage resp = await infoClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

            acceptRange = resp.Headers.AcceptRanges.Count > 0;

            if (resp.Content.Headers.ContentLength != null || resp.StatusCode == HttpStatusCode.OK)
            {
                long size = resp.Content.Headers.ContentLength != null ? resp.Content.Headers.ContentLength.Value : 0;
                DownloadUriInfo uriInfo = new DownloadUriInfo(acceptRange, new UnitValue(size), resp.RequestMessage?.RequestUri, null);
                return uriInfo;
            }
            return null;
        }
        public HttpDownloadCore(ConfigViewModel? viewModel)
        {
            if (viewModel == null)
                this.ConfigViewModel = new ConfigViewModel();
            else
                this.ConfigViewModel = viewModel;
        }
        public void ImportHeader(List<string>? headers)
        {
            if (!IsWorking)
            {
                if (headers != null)
                {
                    if (httpClient != null)
                    {
                        foreach (var header in headers)
                        {
                            string[] headerParts = header.Split(':');
                            httpClient.DefaultRequestHeaders.Add(headerParts[0], headerParts[1]);
                        }
                        workingHeaders = headers;
                        corePrepared = true;
                    }
                    else
                    {
                        workingHeaders = headers;
                        corePrepared = false;
                    }
                }
                else
                {
                    headers = CoreFactory.NecessaryHeaders;
                    this.ImportHeader(headers);
                }
            }
        }
        private void AddRangeHeader(long from, long to)
        {
            if (!IsWorking && httpClient != null)
            {
                httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue();
                httpClient.DefaultRequestHeaders.Range.Unit = "bytes";
                if (to != 0)
                    httpClient.DefaultRequestHeaders.Range.Ranges.Add(new RangeItemHeaderValue(from, to));
                else
                    httpClient.DefaultRequestHeaders.Range.Ranges.Add(new RangeItemHeaderValue(from, null));
            }
        }
        public void TunnelBy(ProxyViewModel? proxyViewModel)
        {
            if (!IsWorking)
            {
                if (proxyViewModel is ProxyViewModel)
                {
                    switch (proxyViewModel.ProxyType)
                    {
                        case ProxyType.None:
                            httpHandler = new HttpClientHandler()
                            {
                                CookieContainer = new CookieContainer(),
                                UseCookies = true,
                                UseProxy = true,
                                AllowAutoRedirect = true,
                                MaxConnectionsPerServer = (int)ConfigViewModel.Connections
                            };
                            break;
                        case ProxyType.Http:
                            HttpClientHandler httpClientProxy = new HttpClientHandler() { Proxy = new WebProxy(proxyViewModel.ProxyAddress, (int)proxyViewModel.Port) };
                            if (proxyViewModel.User != "")
                            {
                                httpClientProxy.Proxy.Credentials = new NetworkCredential(proxyViewModel.User, proxyViewModel.Pass);
                            }
                            httpClientProxy.CookieContainer = new CookieContainer();
                            httpClientProxy.UseCookies = true;
                            httpClientProxy.UseProxy = true;
                            httpClientProxy.AllowAutoRedirect = true;
                            httpClientProxy.MaxConnectionsPerServer = (int)ConfigViewModel.Connections;
                            httpHandler = httpClientProxy;
                            break;
                            //case ProxyType.Socks4: N: support for socks proxies currently suspended
                            //    ProxyClientHandler<Socks4> socks4ClientHandler = new ProxyClientHandler<Socks4>(new ProxySettings() { Host = proxyViewModel.ProxyAddress, Port = (int)proxyViewModel.Port, Credentials = new NetworkCredential(proxyViewModel.User, proxyViewModel.Pass) })
                            //    {
                            //        UseCookies = true,
                            //        CookieContainer = new CookieContainer()
                            //    };
                            //    httpHandler = socks4ClientHandler;
                            //    break;
                            //case ProxyType.Socks5:
                            //    ProxyClientHandler<Socks5> socks5ClientHandler = new ProxyClientHandler<Socks5>(new ProxySettings() { Host = proxyViewModel.ProxyAddress, Port = (int)proxyViewModel.Port, Credentials = new NetworkCredential(proxyViewModel.User, proxyViewModel.Pass) })
                            //    {
                            //        UseCookies = true,
                            //        CookieContainer = new CookieContainer()
                            //    };
                            //    httpHandler = socks5ClientHandler;
                            //    break;
                    }
                    corePrepared = false;
                }
                else
                {
                    throw new ArgumentNullException("proxyViewModel was null");
                }
            }
        }
        private void CreateHttpClient()
        {
            if (!corePrepared)
            {
                if (httpHandler == null)
                    httpHandler = new HttpClientHandler();
                httpClient = new HttpClient(httpHandler);
                this.ImportHeader(workingHeaders);
                corePrepared = true;
            }
        }
        public async Task<bool> GetFrom(long offset, long fileSize)
        {
            if (!corePrepared)
                CreateHttpClient();

            if (!IsWorking && httpClient != null)
            {
                if ((ResumeSupport && !Completed))
                {
                    AddRangeHeader(offset, fileSize);
                }
                httpClient.BaseAddress = new Uri(ConfigViewModel.model.CurrentUrl);
                HttpResponseMessage responseMessage = await httpClient.GetAsync(ConfigViewModel.model.CurrentUrl, HttpCompletionOption.ResponseHeadersRead);
                if ((ensure200 && responseMessage.StatusCode == HttpStatusCode.OK) || !ensure200 || (ensure200 && responseMessage.StatusCode == HttpStatusCode.PartialContent))
                {
                    Stream siteContentStream = await responseMessage.Content.ReadAsStreamAsync();
                    IsWorking = true;
                    corePrepared = true;
                    this.receivedBytes = offset;
                    siteContentStream.BeginRead(buffer, 0, buffer.Length, readCallBack, siteContentStream);
                    return true;
                }
                else
                {
                    throw new Exception("Site did not return in http 200");
                }
            }
            else
            {
                return false;
            }
        }
        public async Task<byte[]?> GetBytes(int offset, int length)
        {
            if (this.ResumeSupport)
            {
                if (!corePrepared)
                    CreateHttpClient();
                if (httpClient != null)
                {

                    this.AddRangeHeader(offset, offset + length);
                    HttpResponseMessage? resp = await httpClient.GetAsync(ConfigViewModel.model.CurrentUrl, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
                    this.resetClient();
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new Exception("Site did not return in http 200");
                    }
                    else
                    {
                        byte[] buffer = new byte[length];
                        Stream? contentStream = resp.Content.ReadAsStream();
                        if (contentStream.Read(buffer, 0, length) > 0)
                        {
                            return buffer;
                        }
                        else
                            return null;
                    }
                }
                else
                {
                    throw new NullReferenceException("HttpClient was null");
                }
            }
            else
            {
                throw new Exception("This server does not support resume");
            }
        }
        private async void readCallBack(IAsyncResult ar)
        {
            if (IsWorking)
            {
                await Task.Run(new Action(() =>
                {
                    Stream? downloadStream = (Stream?)ar.AsyncState;
                    int readBytes = downloadStream?.EndRead(ar) ?? 0;
                    if (readBytes > 0)
                    {
                        agoReceivedBytes = receivedBytes;
                        receivedBytes += readBytes;
                        this.PerformWrite(readBytes);
                        downloadStream?.BeginRead(buffer, 0, buffer.Length, readCallBack, downloadStream);
                    }
                    else if (readBytes == 0)
                    {
                        completed();
                    }
                }));
            }
        }

        private void PerformWrite(int writeSize)
        {
            if (IsWorking && OnDataReceived != null)
            {
                OnDataReceived(this, new DataReceivedEventArgs(receivedBytes, agoReceivedBytes, writeSize, buffer));
            }
        }

        private void completed()
        {
            if (this.OnCompleted != null)
                OnCompleted(this, new EventArgs());
            this.Completed = true;
        }

        public void Pause()
        {
            IsWorking = false;
        }
        private void resetClient()
        {
            if (!IsWorking)
            {
                this.httpClient = null;
                corePrepared = false;
            }
        }
    }
}
