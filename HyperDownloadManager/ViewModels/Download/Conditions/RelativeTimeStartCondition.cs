using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Utils;

namespace HyperDownloadManager.ViewModels.Download.Conditions
{
    public class RelativeTimeStartCondition : StartCondition
    {
        public RelativeTimeStartCondition(DownloadViewModel downloadView) : base(downloadView)
        {
            this.CreationTime = ParentDownloadViewModel.CreationDate;
        }

        public DateTime CreationTime { get; set; }
        public TimeSpan StartIn { get; set; }
        public override bool Ready(DownloadViewModel downloadViewModel)
        {
            Completed = IOUtility.CalculatePercent((long)StartIn.TotalSeconds, (long)(DateTime.Now - CreationTime).TotalSeconds);
            return (CreationTime + StartIn) < DateTime.Now;
        }
    }
}
