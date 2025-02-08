using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Conditions
{
    public class DownloadCompletedCondition : StartCondition
    {
        public DownloadCompletedCondition(DownloadViewModel downloadView) : base(downloadView)
        {
        }

        public DownloadViewModel DownloadView { get; set; } = new DownloadViewModel();
        public DownloadState dlState { get; set; }
        public override bool Ready(DownloadViewModel downloadViewModel)
        {
            if (DownloadView == null || !DownloadManager.DownloadViewModels.Contains(DownloadView))
            {
                return true;
            }
            downloadViewModel.StartTime = DateTime.Now + DownloadView.RemainingTime;
            Completed = DownloadView.CurrentPercent;
            return DownloadView.CurrentState == dlState;
        }
    }
}
