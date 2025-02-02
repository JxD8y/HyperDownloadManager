using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download
{
    public class DataReceivedEventArgs : EventArgs
    {
        public long ReceivedBytes { get; set; }
        public long AgoReceivedBytes { get; set; }
        public int DataLength { get; set; }
        public byte[]? Data { get; set; }
        public DataReceivedEventArgs(long received, long agoReceived, int dataLength, byte[] data)
        {
            ReceivedBytes = received;
            Data = data;
            DataLength = dataLength;
            AgoReceivedBytes = agoReceived;
        }
    }
}
