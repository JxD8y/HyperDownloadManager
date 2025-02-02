using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Container.Condition
{
    public class RelativeTimeContainerStartCondition:ContainerStartCondition
    {
        public DateTime CreationTime { get; set; }
        public TimeSpan StartIn { get; set; }
        public RelativeTimeContainerStartCondition(DateTime creationTime, TimeSpan startIn)
        {
            CreationTime = creationTime;
            StartIn = startIn;
        }
        public override bool Ready(ContainerViewModel dcm)
        {
            dcm.CompleteTime = DateTime.Now.TimeOfDay - StartIn;
            return (CreationTime + StartIn) < DateTime.Now;
        }
    }
}
