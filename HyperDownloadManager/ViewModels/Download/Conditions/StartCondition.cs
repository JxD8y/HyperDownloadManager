using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Conditions
{
    public abstract class StartCondition
    {
        public DownloadViewModel? ParentDownloadViewModel { get; set; }
        public float Completed { get { return ParentDownloadViewModel.Current_Percent; } set { ParentDownloadViewModel.Current_Percent = value; } }
        public virtual bool Ready(DownloadViewModel downloadViewModel)
        {
            throw new NotImplementedException();
        }
        public async Task WaitUntilDone(DownloadViewModel downloadViewModel, CancellationToken cnTk)
        {
            while (!Ready(downloadViewModel))
            {
                cnTk.ThrowIfCancellationRequested();
                Task.Delay(1000).Wait(cnTk);
            }
        }
        public StartCondition(DownloadViewModel downloadView)
        {
            ParentDownloadViewModel = downloadView;
        }
    }
}
