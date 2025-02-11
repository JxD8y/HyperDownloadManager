using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Conditions
{
    public abstract class StartCondition
    {
        public DownloadViewModel ParentDownloadViewModel { get; set; }
        public float Completed { get { return ParentDownloadViewModel.CurrentPercent; } set { ParentDownloadViewModel.CurrentPercent = value; } }
        public virtual bool Ready(DownloadViewModel downloadViewModel)
        {
            throw new NotImplementedException();
        }
        public void WaitUntilDone(DownloadViewModel downloadViewModel, CancellationToken cnTk)
        {
            while (!Ready(downloadViewModel))
            {
                try
                {
                    cnTk.ThrowIfCancellationRequested();
                    Task.Delay(1000).Wait(cnTk);
                }
                catch { break; }
            }
        }
        public StartCondition(DownloadViewModel downloadView)
        {
            ParentDownloadViewModel = downloadView;
        }
    }
}
