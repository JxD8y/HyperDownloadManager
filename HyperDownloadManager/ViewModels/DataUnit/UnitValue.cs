using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.DataUnit
{
    public struct UnitValue
    {
        public UnitValue(Unit dataUnit, float dataValue, long originD = 0L)
        {
            DataUnit = dataUnit;
            DataValue = dataValue;
            OriginData = originD;
        }
        public UnitValue(long originD)
        {
            UnitValue uvm = UnitConverter.DetectConvert(originD);
            DataUnit = uvm.DataUnit;
            DataValue = uvm.DataValue;
            OriginData = originD;
        }
        public long OriginData { get; set; }
        public Unit DataUnit { get; set; }
        public float DataValue { get; set; }
    }
}
