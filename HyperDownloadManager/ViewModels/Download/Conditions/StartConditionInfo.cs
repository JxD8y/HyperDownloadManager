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
            AutoType = AutoStartConditionType.Instant;
        }
        private AutoStartConditionType triggerType;
        private DateTime startAt;
        private TimeSpan startIn;
        private int downloadId;
        private int containerId;
        private DownloadState downloadState;
        public AutoStartConditionType AutoType { get { return triggerType; } set { triggerType = value; OnPropertyChanged(); } }
        public DateTime StartAt { get { return startAt; } set { startAt = value; OnPropertyChanged(); } }
        public TimeSpan StartIn { get { return startIn; } set { startIn = value; OnPropertyChanged(); } }
        public int DownloadId { get { return downloadId; } set { downloadId = value; OnPropertyChanged(); } }
        public int ContainerId { get { return containerId; } set { containerId = value; OnPropertyChanged(); } }
        public DownloadState dlState { get { return downloadState; } set { downloadState = value; OnPropertyChanged(); } }
        public static bool operator ==(StartConditionInfo left, StartConditionInfo right)
        {
            if (left.triggerType == right.triggerType)
            {
                switch (left.AutoType)
                {
                    case AutoStartConditionType.Instant:
                        return true;
                    case AutoStartConditionType.AbsoluteTime:
                        if (left.startAt == right.startAt)
                            return true; break;
                    case AutoStartConditionType.RelativeTime:
                        if (left.startIn == right.startIn)
                            return true;
                        break;
                    case AutoStartConditionType.DownloadStateChange:
                        if (left.downloadId == right.downloadId)
                            return true;
                        break;
                    case AutoStartConditionType.AllDownloadFinish:
                        return true;
                    //case AutoStartConditionType.ContainerFinish:
                    //    if (left.containerId == right.containerId)
                    //        return true;
                    //    break;
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
