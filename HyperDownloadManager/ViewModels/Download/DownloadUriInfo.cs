using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;

namespace HyperDownloadManager.ViewModels.Download
{
    public class DownloadUriInfo
    {
        public bool Resumable { get; set; }
        public UnitValue Size { get; set; }
        public Uri? Url { get; set; }
        public BitmapSource? Icon { get; set; }
        public long Ping { get; set; }
        public string FileName { get; set; } = "";
        public DownloadUriInfo() { }
        public DownloadUriInfo(bool resumable,UnitValue size,Uri? url,BitmapSource? icon)
        {
            this.Resumable = resumable;
            this.Size = size;
            this.Url = url;
            this.Icon = icon;
            this.Icon = icon;

            this.FileName = System.IO.Path.GetFileName(this.Url?.LocalPath) ?? "";
            if (GlobalSupervisor.GeneralSettingsViewModel.SaveTemp)
                this.FileName += "." + IOUtility.TempExtension;
        }

    }
}
