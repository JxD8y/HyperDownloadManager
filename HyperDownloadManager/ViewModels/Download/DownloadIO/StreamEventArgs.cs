using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.DownloadIO
{
    public class StreamEventArgs : EventArgs
    {
        public int Offset { get; set; }
        public long Length { get; set; }

        public StreamEventArgs(int offset, long length)
        {
            Offset = offset;
            Length = length;
        }
    }
}
