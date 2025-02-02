using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.DataUnit;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Download.DownloadIO
{
    public class IOCore:ViewModel //N: This will change after first success build!
    {
        #region Properties
        private int FSID = -1;
        [BsonIgnore]
        public IoState IoState { get; set; } = IoState.FileNotOpen;
        [BsonIgnore]
        public ThrottledStream? fileStream { get; set; }
        [BsonIgnore]
        public bool isStreamOpen { get; set; } = false;
        public bool isTempFile { get; set; } = false;
        public UnitValue fileSize { get; set; } = new UnitValue(0);
        public string? fileName { get; set; }
        public string? saveDirectory { get; set; }
        public string? fileExtension { get; set; }
        public bool Resumable { get; set; }
        private long ping = -1;
        public long Ping { get { return ping; } set { ping = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public BitmapSource? Icon { get; set; }
        public Uri? Url { get; set; }
        [BsonIgnore]
        public DownloadViewModel? DownloadViewModel { get; set; }
        public string? fileSavePath
        {
            get
            {
                if (saveDirectory != null && fileName != null)
                {
                    string path = Path.Combine(saveDirectory, fileName);
                    if (isTempFile)
                        return Path.Combine(path, $".{IOUtility.TempExtension}");
                    else
                        return path;
                }
                return "";
            }
            set
            {
                saveDirectory = Path.GetDirectoryName(value);
                fileName = Path.GetFileName(value);
                fileExtension = Path.GetExtension(value);
            }
        }
        public event EventHandler<EventArgs>? OnFileMaxSize;
        #endregion
        public IOCore() { }
        public IOCore(string savePath, bool TempFile, long fileSize, DownloadViewModel downloadViewModel,IOCore? dataCarrier)
        {
            fileSavePath = savePath;
            isTempFile = TempFile;
            DownloadViewModel = downloadViewModel;
            this.fileSize = UnitConverter.DetectConvert(fileSize);
            fileName = Path.GetFileName(fileSavePath);
            fileExtension = Path.GetExtension(fileSavePath);
            if (dataCarrier != null)
            {
                this.Resumable = dataCarrier.Resumable;
                this.Url = dataCarrier.Url;
            }
            if (DownloadViewModel.ConfigViewModel != null)
                DownloadViewModel.ConfigViewModel.ConfigUpdated += ConfigViewModel_ConfigUpdated;
        }

        private void ConfigViewModel_ConfigUpdated(object? sender, List<object> e)
        {
            if (e.Where((obj) => { string a = obj.GetType().GetProperties().First().Name; if (a == "MaxFileSize" || a == "SpeedLimit") return true; else return false; }).Count() != 0)
            {
                this.CloseFile();
                this.OpenFile();
            }
        }
        public void ResetFileData()
        {
            IOUtility.CloseStream(FSID);
            File.Delete(fileSavePath);
            File.Create(fileSavePath).Dispose();
            this.OpenFile();
        }
        public void ResetSupervisor()
        {
            if (isStreamOpen)
            {
                fileStream?.Flush();
                fileStream.Position = 0;
            }
            else
            {
                OpenFile();
                ResetSupervisor();
            }
        }
        public void ClearTempFile()
        {
            if (isTempFile)
            {
                if (IoState == IoState.FileOk)
                {
                    CloseFile();
                    isTempFile = false;
                    File.Move(saveDirectory, Path.Combine(saveDirectory, fileName));
                }
            }
            else
            {
                this.CloseFile();
            }
        }
        #region StreamBaseFunctions
        public void OpenFile()
        {
            if (fileName == null) throw new FileNotFoundException("File not found.");
            if (!isStreamOpen)
            {
                ThrottledStream? stream = null;
                stream = IOUtility.CreateStream(fileSavePath, DownloadViewModel.ConfigViewModel.SpeedLimit, DownloadViewModel.ConfigViewModel.MaxFileSize, FileMode.Open);
                if (stream != null)
                {
                    this.FSID = IOUtility.GetStreamID(stream);
                    isStreamOpen = true;
                    IoState = IoState.FileOk;
                    this.fileStream = stream;
                    this.fileStream.Position = this.fileStream.Length;
                }
                else
                {
                    isStreamOpen = false;
                    IoState = IoState.FileNotExist;
                    throw new FileNotFoundException("File not found.");
                }
            }
        }
        public void CloseFile()
        {
            if (fileStream != null || isStreamOpen)
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
            if (!isStreamOpen)
            {
                OpenFile();
                writeToFile(data, offset, count);
            }
            if (data == null) throw new ArgumentNullException("data is null");
            else
            {
                fileStream.Position = offset;
                fileStream?.Write(data, 0, count);
                fileStream?.Flush();
                fileStream.Position = fileStream.Length;
            }
        }
        public void writeToFile(byte[] data, int count)
        {
            if (!isStreamOpen)
            {
                OpenFile();
                writeToFile(data, count);
            }
            if (data == null) throw new ArgumentNullException("data is null");
            else
            {
                fileStream?.Write(data, 0, count);
                fileStream?.Flush();
            }
        }
        public byte[] readFileBytes(int offset, int length)
        {
            byte[] bytes = new byte[length];
            fileStream.Position = offset;
            fileStream?.Read(bytes, 0, length);
            fileStream.Position = fileStream.Length;
            return bytes;
        }
        #endregion
        #region StaticMembers
        public static IOCore? CreateInfoSupervisor(long fileSize, bool resumable, string savePath, Uri url, bool saveTemp)
        {
            IOCore viewModel = new IOCore();
            viewModel.fileSize = UnitConverter.DetectConvert(fileSize);
            viewModel.Resumable = resumable;
            if (url != null)
            {
                string fileName = Path.GetFileName(url.LocalPath);
                viewModel.fileSavePath = Path.Combine(savePath, fileName);
                viewModel.isTempFile = saveTemp;
                if (string.IsNullOrEmpty(viewModel.fileName))
                {
                    viewModel.fileName = url.LocalPath.Replace("\\", "").Replace("/", "");
                    viewModel.fileExtension = "";
                }
                viewModel.Url = url;
                return viewModel;
            }
            return null;
        }
        public static bool IsValidInfoCarrier(IOCore? iOSupervisor)
        {
            if (iOSupervisor == null)
                return false;
            return true;
        }
        #endregion
    }
}
