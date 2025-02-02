using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.DataUnit
{
    public static class UnitConverter
    {
        public const long MB = 1048576;
        public const long KB = 1024;
        public const long GB = 1073741824;
        public const long TB = 1099511627776;

        public const long MBit = 8388608;
        public const long GBit = 8589934592;
        public const long TBit = 8796093022208;
        public const long KBit = 8192;
        #region Converters
        public static float ToKb(long bt)
        {
            return (float)((float)bt / 1024);
        }
        public static long FromKb(float kb)
        {
            return (long)(kb * 1024);
        }
        public static float ToMb(long bt)
        {
            return (float)((float)bt / 1024 / 1024);
        }
        public static long FromMb(float mb)
        {
            return (long)(mb * 1024 * 1024);
        }
        public static float ToGb(long bt)
        {
            return (float)((float)bt / 1024 / 1024 / 1024);
        }
        public static long FromGb(float gb)
        {
            return (long)(gb * 1024 * 1024 * 1024);
        }
        public static float ToTb(long bt)
        {
            return (float)((float)bt / 1024 / 1024 / 1024 / 1024);
        }
        public static long FromTb(float tb)
        {
            return (long)(tb * 1024 * 1024 * 1024 * 1024);
        }
        #endregion
        public static Unit DetectUnit(long bt)
        {
            if (bt < 1024) { return Unit.Byte; }
            if (bt > 1024 && bt <= 1048576) { return Unit.Kb; }
            if (bt >= MB && bt <= 1073741824) { return Unit.Mb; }
            if (bt >= GB && bt <= 1099511627776) { return Unit.Gb; }
            if (bt >= TB && bt <= 1.125900e+15) { return Unit.Tb; }
            return Unit.NON;
        }
        public static UnitValue DetectConvert(long sz)
        {
            float conv = 0;
            Unit fileUnit = DetectUnit(sz);
            if (GlobalSupervisor.GeneralSettingsViewModel?.MinUnitPrefix >= fileUnit)
            {
                fileUnit = GlobalSupervisor.GeneralSettingsViewModel.MinUnitPrefix;
            }
            switch (fileUnit)
            {
                case Unit.Kb:
                    conv = ToKb(sz);
                    break;
                case Unit.Mb:
                    conv = ToMb(sz);
                    break;
                case Unit.Gb:
                    conv = ToGb(sz);
                    break;
                case Unit.Tb:
                    conv = ToTb(sz);
                    break;
                default:
                    conv = ToMb(sz);
                    break;
            }
            return new UnitValue(fileUnit, conv, sz);
        }
        public static float ConvertTo(Unit DestinationUnit, Unit SourceUnit, float value)
        {
            long byteP = 0;
            switch (SourceUnit)
            {
                case Unit.Kb:
                    byteP = FromKb(value);
                    break;
                case Unit.Mb:
                    byteP = FromMb(value);
                    break;
                case Unit.Gb:
                    byteP = FromGb(value);
                    break;
                case Unit.Tb:
                    byteP = FromTb(value);
                    break;
            }
            if (byteP != 0)
            {
                switch (DestinationUnit)
                {
                    case Unit.Kb:
                        return ToKb(byteP);
                    case Unit.Mb:
                        return ToMb(byteP);
                    case Unit.Gb:
                        return ToGb(byteP);
                    case Unit.Tb:
                        return ToTb(byteP);
                }
            }
            return 0;
        }
        public static long ConvertToByte(Unit SourceUnit, float value)
        {
            long byteP = 0;
            switch (SourceUnit)
            {
                case Unit.Kb:
                    byteP = FromKb(value);
                    break;
                case Unit.Mb:
                    byteP = FromMb(value);
                    break;
                case Unit.Gb:
                    byteP = FromGb(value);
                    break;
                case Unit.Tb:
                    byteP = FromTb(value);
                    break;
            }
            return byteP;
        }
    }
}
