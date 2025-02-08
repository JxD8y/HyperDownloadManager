using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Utils;

namespace HyperDownloadManager.ViewModels.Download.Conditions
{
    public class AbsoluteTimeStartCondition : StartCondition
    {
        public AbsoluteTimeStartCondition(DownloadViewModel downloadView) : base(downloadView)
        {
        }
        public DateTime StartAt { get; set; }
        public override bool Ready(DownloadViewModel downloadViewModel)
        {
            Completed = (float)(((float)((long)(StartAt - ParentDownloadViewModel.CreationDate).TotalSeconds) / (float)((long)(DateTime.Now - ParentDownloadViewModel.CreationDate).TotalSeconds)) * 100);
            return StartAt < DateTime.Now;
        }
    }
}
