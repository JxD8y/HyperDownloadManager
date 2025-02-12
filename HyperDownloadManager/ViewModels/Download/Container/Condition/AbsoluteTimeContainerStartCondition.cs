using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Container.Condition
{
    public class AbsoluteTimeContainerStartCondition:ContainerStartCondition
    {
        public DateTime StartAt { get; set; }
        public AbsoluteTimeContainerStartCondition(DateTime startAt)
        {
            StartAt = startAt;
        }
        public override bool Ready(ContainerViewModel viewModel)
        {
            return StartAt < DateTime.Now;
        }
    }
}
