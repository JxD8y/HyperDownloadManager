using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Utils;

namespace HyperDownloadManager.ViewModels.Proxy
{
    public class ProxyViewModel : ViewModel
    {
        public ProxyViewModel()
        {
            ProxyType = ProxyType.None;
            ProxyAddress = "";
            User = "";
            Pass = "";
        }
        public ProxyType ProxyType { get; set; }
        public string ProxyAddress { get; set; } = "";
        public uint Port { get; set; } = 0;
        public string User { get; set; } = "";
        public string Pass { get; set; } = "";
        public long Ping
        {
            get
            {
                if (!string.IsNullOrEmpty(ProxyAddress))
                    return NetworkUtility.GetServerPing(ProxyAddress);
                else
                    return -1;
            }
        }
        public bool ContainsProxy()
        {
            if (ProxyType != ProxyType.None && ProxyAddress != "")
                return true;
            else
                return false;
        }
        public bool IsOnline()
        {
            return NetworkUtility.CheckProxy(this);
        }
        public static bool operator ==(ProxyViewModel left, ProxyViewModel right)
        {
            return (left.ProxyType == right.ProxyType) && (left.Port == right.Port) && (left.ProxyAddress == right.ProxyAddress);
        }
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator !=(ProxyViewModel left, ProxyViewModel right)
        {
            return !(left == right);
        }
        public static ProxyViewModel? operator &(ProxyViewModel left, ProxyViewModel right)
        {
            if (left.ContainsProxy())
                return left;
            else if (right.ContainsProxy())
                return right;
            else return null;
        }
    }
}
