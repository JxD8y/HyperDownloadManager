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
        public virtual bool Ready(ContainerViewModel viewModel)
        {
            throw new NotImplementedException();
        }
        public async Task Wait(ContainerViewModel viewModel, CancellationToken cancelToken)
        {
            await Task.Run(() =>
            {
                while (!Ready(viewModel) && !_stop)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    Task.Delay(1000).Wait(cancelToken);
                }
            });
        }
    }
}
