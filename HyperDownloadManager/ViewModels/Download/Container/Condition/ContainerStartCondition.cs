using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Container.Condition
{
    public abstract class ContainerStartCondition
    {
        public virtual bool Ready(ContainerViewModel viewModel)
        {
            throw new NotImplementedException();
        }
        public void Wait(ContainerViewModel viewModel, CancellationToken cancelToken)
        {
            while (!Ready(viewModel))
            {
                cancelToken.ThrowIfCancellationRequested();
                Task.Delay(1000).Wait(cancelToken);
            }
        }
    }
}
