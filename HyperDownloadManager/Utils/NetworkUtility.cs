using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.ViewModels.Proxy;

namespace HyperDownloadManager.Utils
{
    public static class NetworkUtility
    {
        private static bool Observing = false;
        private static CancellationTokenSource ConnectionCheckerCancellationToken = new CancellationTokenSource();
        public static string DefaultPingHost1 = "4.2.2.4";
        public static string DefaultPingHost2 = "google.com";
        public const long BUFFERSIZE = 2048;
        public static event EventHandler<bool>? OnNetworkConnectivityChanged;

        #region Info
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool CheckConnection()
        {
            bool returnValue = false;
            try
            {
                int Desc;
                returnValue = InternetGetConnectedState(out Desc, 0);
            }
            catch
            {
                returnValue = false;
            }
            return returnValue;
        }
        public static void StartNetworkConnectivityObservation()
        {
            if (!Observing)
            {
                ConnectionCheckerCancellationToken = new CancellationTokenSource();
                Task.Run(() =>
                {
                    while (!ConnectionCheckerCancellationToken.IsCancellationRequested)
                    {
                        bool connection = CheckConnection();
                        if (OnNetworkConnectivityChanged != null)
                        {
                            OnNetworkConnectivityChanged(null, connection);
                        }
                        Task.Delay(3000);
                    }
                });
                Observing = true;
            }
        }
        public static void StopNetworkConnectivityObservation()
        {
            ConnectionCheckerCancellationToken.Cancel();
            Observing = false;
        }
        #endregion
        #region Utils
        public static bool TryConvertToUri(string url, out Uri _url)
        {
            bool isurl = Uri.IsWellFormedUriString(url, UriKind.Absolute);
            if (isurl)
            {
                Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _url);
            }
            else
                _url = null;
            return isurl;
        }
        public static bool isUrl(string url)
        {
            if (Regex.IsMatch(url, @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}\b") || Regex.IsMatch(url, @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)") || Regex.IsMatch(url, @"^([a-zA-Z0-9](?:(?:[a-zA-Z0-9-]*|(?<!-)\.(?![-.]))*[a-zA-Z0-9]+)?)$"))
            {
                return true;
            }
            return false;
        }
        public static bool isUrl(string url, out Uri uri)
        {
            uri = null;
            if (Regex.IsMatch(url, @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}\b") || Regex.IsMatch(url, @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)") || Regex.IsMatch(url, @"^([a-zA-Z0-9](?:(?:[a-zA-Z0-9-]*|(?<!-)\.(?![-.]))*[a-zA-Z0-9]+)?)$"))
            {
                return true && Uri.TryCreate(url, UriKind.Absolute, out uri);
            }
            return false;
        }
        public static long GetServerPing(string server)
        {
            try
            {
                Ping _png = new Ping();
                PingReply rp = _png.Send(server, 1000);
                return rp.RoundtripTime;
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Utils, $"{ex.Message} in sending PingRequest");
                return -1;
            }
        }
        public static string ListToString(List<string> list)
        {
            string merged = "";
            foreach(string item in list)
            {
                merged += item + '\n';
            }
            return merged;
        }
        internal static bool CheckProxy(ProxyViewModel proxyViewModel)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
