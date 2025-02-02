using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.DataUnit
{
    public struct SpeedViewModel
    {
        public float Speed { get; set; }
        public TimeSpan Time { get; set; }
        public SpeedViewModel(float speed, TimeSpan time)
        {
            Speed = speed;
            Time = time;
        }
    }
}
