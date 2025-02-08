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
        public DateTime CreationDate { get; set; }
        public AbsoluteTimeContainerStartCondition(DateTime creationDate, DateTime startAt)
        {
            CreationDate = creationDate;
            StartAt = startAt;
        }
        public override bool Ready(ContainerViewModel viewModel)
        {
            viewModel.CompleteTime = StartAt - CreationDate;
            return StartAt < DateTime.Now;
        }
    }
}
