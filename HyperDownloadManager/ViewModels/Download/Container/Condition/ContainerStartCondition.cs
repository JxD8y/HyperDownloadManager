using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Container.Condition
{
    public abstract class ContainerStartCondition
    {
        private bool _stop = false;
        public void Kill()
        {
            _stop = true;
        }
        public virtual bool Ready(ContainerViewModel downloadViewModel)
        {
            throw new NotImplementedException();
        }
        public async Task Wait(ContainerViewModel downloadViewModel, CancellationToken cnTk)
        {
            await Task.Run(() =>
            {
                while (!Ready(downloadViewModel) && !_stop)
                {
                    cnTk.ThrowIfCancellationRequested();
                    Task.Delay(1000).Wait(cnTk);
                }
            });
        }
    }
}
