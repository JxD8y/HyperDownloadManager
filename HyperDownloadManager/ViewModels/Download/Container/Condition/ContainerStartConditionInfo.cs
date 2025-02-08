using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download.Container.Condition
{
    public class ContainerStartConditionInfo
    {
        public TimeSpan StartAt { get; set; }
        public DateTime StartIn { get; set; }
        public ContainerStartMode StartMode { get; set; }
        [BsonIgnore]
        public ContainerStartCondition? StartCondition { get; set; }
        [BsonCtor]
        public ContainerStartConditionInfo() { }
        public ContainerStartConditionInfo(ContainerStartMode startMode)
        {
            StartMode = startMode;
        }

    }
}
