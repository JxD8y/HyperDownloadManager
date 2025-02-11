using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.ViewModels.Download.DownloadIO;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;

namespace HyperDownloadManager.Utils
{
    public static class IOUtility
    {
        public static Dictionary<int, ThrottledStream> Streams = new Dictionary<int, ThrottledStream>();
        public const int MaxStreamCount = 10000;
        public const string TempExtension = "DDL";
        public static ThrottledStream? CreateStream(string filePath, long maxBytePreSecond, long maxFileSize, FileMode fileMode)
        {
            try
            {
                int id = GlobalSupervisor.GetRandom(MaxStreamCount);
                Stream stream = File.Open(filePath, fileMode);
                ThrottledStream throttledStream = new ThrottledStream(stream, maxBytePreSecond, maxFileSize, filePath);
                Streams.Add(id, throttledStream);
                return throttledStream;
            }
            catch (FileNotFoundException)
            {
                return CreateStream(filePath, maxBytePreSecond, maxFileSize, FileMode.OpenOrCreate);
            }
        }
        public static void CloseStream(int streamId)
        {
            if (StreamExist(streamId))
            {
                ThrottledStream? throttledStream = GetThrottledStream(streamId);
                throttledStream?.Close();
                Streams.Remove(streamId);
            }
        }
        public static bool StreamExist(int streamId)
        {
            return Streams.ContainsKey(streamId);
        }
        public static bool StreamExist(ThrottledStream? throttledStream)
        {
            if (throttledStream == null)
                return false;
            else
            {
                return Streams.ContainsValue(throttledStream);
            }
        }
        public static bool CheckFileOpen(string filePath)
        {
            foreach (ThrottledStream throttledStream in Streams.Values)
            {
                if (throttledStream.FilePath == filePath)
                {
                    return true;
                }
            }
            return false;
        }
        public static ThrottledStream? GetThrottledStream(string filePath)
        {
            foreach (ThrottledStream throttledStream in Streams.Values)
            {
                if (throttledStream.FilePath == filePath)
                {
                    return throttledStream;
                }
            }
            return null;
        }
        public static int GetStreamID(ThrottledStream? throttledStream)
        {
            if (throttledStream == null)
                throw new ArgumentNullException("Stream is null");
            else
            {
                return Streams.Where((k, s) => { return throttledStream.FilePath == k.Value.FilePath; }).First().Key;
            }
        }
        public static ThrottledStream? GetThrottledStream(int id)
        {
            if (Streams.ContainsKey(id))
            {
                return Streams[id];
            }
            else
            {
                return null;
            }
        }
        public static BitmapSource? GetFileIcon(string fileName)
        {
            if(fileName != "")
            {
                Icon? icon = null;
                string path = Path.Combine(SpecialDirectories.Temp, fileName);
                File.Create(path).Close();
                icon = Icon.ExtractAssociatedIcon(path);
                File.Delete(path);
                if (icon == null)
                    return null;
                BitmapSource bitmap = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                return bitmap;
            }
            return null;
        }
        public static void OpenExplorer(string? fileSavePath)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"explorer.exe /select , {fileSavePath}"));
        }

        internal static float CalculatePercent(long part, long all)
        {
            return (float)(((float)part / (float)all) * 100);
        }

        public static string AdjustFileNameString(string path)
        {
            if (path.Length <= 30)
                return path;
            else
            {
                string ext = Path.GetExtension(path);
                string AdjustedPath = "";
                Match m1 = Regex.Match(path, "(.{1,20})");
                if (m1.Success)
                {
                    string shortedFileName = m1.Groups[1].Value;
                    string Part = "";
                    Match m2 = Regex.Match(path, "\\.part(.*)\\.");
                    if (m2.Success)
                    {
                        Part = m2.Groups[1].Value;
                    }
                    if (!string.IsNullOrEmpty(Part))
                    {
                        AdjustedPath = $"{shortedFileName}(...)part{Part}{ext}";
                    }
                    else
                        AdjustedPath = $"{shortedFileName}(...){ext}";

                }
                else
                    AdjustedPath = path;
                return AdjustedPath;
            }
        }
        public static string? GetSystemDownloadFolder()
        {
            try
            {
                return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", string.Empty)?.ToString();
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Download, $"fail to find default download folder: {ex.Message}"); return ""; }
        }
    }
}
