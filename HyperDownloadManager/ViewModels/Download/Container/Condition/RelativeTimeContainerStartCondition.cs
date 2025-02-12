using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Container.Condition
{
    public class RelativeTimeContainerStartCondition:ContainerStartCondition
    {
        public DateTime ReferenceTime { get; set; }
        public TimeSpan StartIn { get; set; }
        public RelativeTimeContainerStartCondition(DateTime referenceTime, TimeSpan startIn)
        {
            ReferenceTime = referenceTime;
            StartIn = startIn;
        }
        public override bool Ready(ContainerViewModel viewModel)
        {
            return (ReferenceTime + StartIn) < DateTime.Now;
        }
    }
}
