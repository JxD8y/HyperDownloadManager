using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.Conditions
{
    public class StartConditionInfo : ViewModel
    {
        public StartConditionInfo()
        {
            ConditionType = AutoStartConditionType.Instant;
        }
        public AutoStartConditionType ConditionType { get; set; }
        public DateTime StartAt { get; set; }
        public TimeSpan StartIn { get; set; }
        public int DownloadId { get; set; }
        public int ContainerId { get; set; }
        public DownloadState DownloadState { get; set; }
        public static bool operator ==(StartConditionInfo left, StartConditionInfo right)
        {
            if (left.ConditionType == right.ConditionType)
            {
                switch (left.ConditionType)
                {
                    case AutoStartConditionType.Instant:
                        return true;
                    case AutoStartConditionType.AbsoluteTime:
                        if (left.ConditionType == right.ConditionType)
                            return true; break;
                    case AutoStartConditionType.RelativeTime:
                        if (left.StartIn == right.StartIn)
                            return true;
                        break;
                    case AutoStartConditionType.DownloadStateChange:
                        if (left.DownloadId == right.DownloadId)
                            return true;
                        break;
                    case AutoStartConditionType.AllDownloadFinish:
                        return true;
                }
                return false;
            }
            else
                return false;
        }
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }
        public static bool operator !=(StartConditionInfo left, StartConditionInfo right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
