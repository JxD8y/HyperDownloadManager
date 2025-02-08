using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Imaging;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
namespace HyperDownloadManager.ViewModels.Download.DownloadIO
{
    public class IOCore
    {
        #region Properties
        private int FSID = -1;
        public IoState IoState { get; set; } = IoState.FileNotOpen;
        public ThrottledStream? fileStream { get; set; }
        public bool isStreamOpen { get; set; } = false;
        public DownloadViewModel model { get; set; } = new DownloadViewModel();
        public long FileSize { get { if (fileStream == null) return 0; else return fileStream.Length; } }

        public event EventHandler<EventArgs>? OnFileMaxSize;
        #endregion
        public IOCore() { }
        public IOCore(DownloadViewModel? viewModel)
        {
            if (viewModel is DownloadViewModel)
            {
                this.model = viewModel;
                if (this.model.ConfigViewModel is ConfigViewModel)
                    this.model.ConfigViewModel.ConfigUpdated += ConfigViewModel_ConfigUpdated;
            }
            else
            {
                throw new ArgumentNullException("DownloadViewModel was null");
            }
        }

        private void ConfigViewModel_ConfigUpdated(object? sender, List<object> e)
        {
            if (e.Where((obj) => { string a = obj.GetType().GetProperties().First().Name;
                if (a == "MaxFileSize" || a == "SpeedLimit") //IO related config properties
                    return true; 
                else return false; }).Count() != 0)
            {
                this.CloseFile();
                this.OpenFile();
            }
        }
        public void ResetFileData()
        {
            if (this.model?.FileSavePath != null)
            {
                IOUtility.CloseStream(FSID);
                File.Delete(this.model.FileSavePath);
                File.Create(this.model.FileSavePath).Dispose();
                this.OpenFile();
            }
            else
            {
                throw new NullReferenceException("File save path in downloadViewModel was null");
            }
        }
        public void Reset()
        {
            if (isStreamOpen)
            {
                if(this.fileStream is ThrottledStream)
                {
                    this.fileStream.Position = 0L;
                    this.fileStream?.Flush();
                }
                else
                {
                    throw new NullReferenceException("fileStream is null");
                }
            }
            else
            {
                OpenFile();
                Reset();
            }
        }
        public void ClearTempFile()
        {
            this.CloseFile();
            if (this.model.IsTempFile)
            {
                if (IoState == IoState.FileOk && (this.model.FileSavePath != null && this.model.CurrentFileName != null && this.model.CurrentSaveFileDirectory != null))
                {
                    File.Move(this.model.FileSavePath, Path.Combine(this.model.CurrentSaveFileDirectory, this.model.CurrentFileName));
                }
            }
        }
        #region StreamBaseFunctions
        public void OpenFile()
        {
            if (this.model.FileSavePath == null)
                throw new FileNotFoundException("File not found");
            if (!isStreamOpen)
            {
                ThrottledStream? stream = null;
                stream = IOUtility.CreateStream(this.model.FileSavePath, this.model.ConfigViewModel.SpeedLimit, this.model.ConfigViewModel.MaxFileSize, FileMode.Open);
                if (stream != null)
                {
                    this.FSID = IOUtility.GetStreamID(stream);
                    isStreamOpen = true;
                    IoState = IoState.FileOk;
                    this.fileStream = stream;
                    this.fileStream.Position = this.fileStream.Length;
                    this.model.DownloadedSize = new UnitValue(this.fileStream.Length);
                }
                else
                {
                    isStreamOpen = false;
                    IoState = IoState.FileNotExist;
                    throw new FileNotFoundException("fail to open stream on desired file path");
                }
            }
        }
        public void CloseFile()
        {
            if (fileStream != null && isStreamOpen)
            {
                IOUtility.CloseStream(FSID);
                this.isStreamOpen = false;
                this.IoState = IoState.FileNotOpen;
                fileStream = null;
            }
        }
        #endregion
        #region StreamFunctions
        public void writeToFile(byte[] data, int offset, int count)
        {
            if (data == null) 
                throw new ArgumentNullException("data is null");
            if (!isStreamOpen || fileStream == null)
                OpenFile();
            else
            {
                try
                {
                    fileStream.Position = offset;
                    fileStream?.Write(data, 0, count);
                    fileStream?.Flush();
                    fileStream.Position = fileStream.Length;
                }
                catch
                {
                    this.CloseFile();
                    throw;
                }
            }
        }
        public void writeToFile(byte[]? data, int count)
        {
            if (data == null) 
                throw new ArgumentNullException("data is null");
            if (!isStreamOpen)
            {
                OpenFile();
                writeToFile(data, count);
            }
            else
            {
                try
                {
                    fileStream?.Write(data, 0, count);
                    fileStream?.Flush();
                }
                catch
                {
                    this.CloseFile();
                    throw;
                }
                
            }
        }
        public byte[] readFileBytes(int offset, int length)
        {
            if (!isStreamOpen || fileStream == null)
                OpenFile();
            try
            {
                byte[] bytes = new byte[length];
                fileStream.Position = offset;
                fileStream?.Read(bytes, 0, length);
                fileStream.Position = fileStream.Length;
                return bytes;
            }
            catch
            {
                this.CloseFile();
                throw;
            }
        }
        #endregion
    }
}
