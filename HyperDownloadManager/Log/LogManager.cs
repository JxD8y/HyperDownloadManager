using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using System.Xml.Serialization;
using HyperDownloadManager.Dialogs;

namespace HyperDownloadManager.Log
{
    public static class LogManager
    {
        private const long MAXSIZE = 1468006L;

        public static string CurrentLogFullname = "";

        private static int logentry_count = 0;

        public static FileStream? CurrentLogStream = null;

        public delegate void Update(LogViewModel lvm);
        public static event Update? OnLogAdd = null;
        private static XmlSerializer xsz = new XmlSerializer(typeof(List<LogViewModel>), new XmlRootAttribute("LogEntries"));

        public static List<LogViewModel> Logs { get; set; } = new List<LogViewModel>();
        public static void Load_Logs(string logs)
        {
            try
            {
                Logs = new List<LogViewModel>();
                CheckExistLogs_UpdateLogEntrycount(logs);
                if (logentry_count >= 0)
                {
                    DirectoryInfo logn = new DirectoryInfo(logs);
                    if (logn.GetFiles().Length >= 0)
                    {
                        int i = 0;
                        foreach (FileInfo log in logn.GetFiles())
                        {
                            if (log.Name.StartsWith("Log-") && log.Length < MAXSIZE)
                            {
                                CurrentLogStream = File.Open(log.FullName, FileMode.Open);
                                CurrentLogFullname = log.FullName;
                                if (!Deserialize()) { continue; }
                                i++;
                            }
                            else { continue; }
                        }
                        if (i == 0) { CreateNewLogEntry(); }
                    }
                }
            }
            catch { }

        }

        private static bool Deserialize()
        {
            try
            {
                object? _logmd = xsz.Deserialize(CurrentLogStream);
                if (_logmd != null)
                {
                    Logs = (List<LogViewModel>)_logmd;
                    if (OnLogAdd != null)
                    {
                        foreach (LogViewModel log in Logs)
                        {
                            OnLogAdd(log);
                        }
                    }
                    return true;
                }
                else { return false; }
            }
            catch { return false; }
        }

        private static bool Serialize()
        {
        jmp:
            if (CurrentLogStream.Length < MAXSIZE)
            {
                CurrentLogStream.Position = 0;

                xsz.Serialize(CurrentLogStream, Logs);
                return true;
            }
            else { if (CreateNewLogEntry()) { Logs = new List<LogViewModel>(); goto jmp; } else { return false; } }
        }
        private static void CheckExistLogs_UpdateLogEntrycount(string logs)
        {
            logentry_count = 0;
            DirectoryInfo logn = new DirectoryInfo(logs);
            if (logn.GetFiles().Length > 0)
            {
                foreach (FileInfo log in logn.GetFiles())
                {
                    if (log.Name.StartsWith("Log-"))
                    {
                        logentry_count++;
                    }
                    else { continue; }
                }
            }
        }
        public static bool Log(MessageLevel lv, LogSection lc, string _log, bool silent = false)
        {
            LogViewModel log = new LogViewModel();
            log.Log = _log;
            log.logLevel = lv;
            log.logsection = lc;
            log.AccureTime = DateTime.Now;
            Logs.Add(log);
            if (OnLogAdd != null) { OnLogAdd(log); }
            if (!silent && lv == MessageLevel.Error)
            {
                _showMessage(log);
            }
            if (Serialize()) { return true; }
            else { return false; }
        }
        private static async void _showMessage(LogViewModel log)
        {
            await DialogManager.ShowMessageBox(log.Log, log.logLevel, ButtonOrder.OK, true);
        }
        public static bool CreateNewLogEntry()
        {
            try
            {
                CheckExistLogs_UpdateLogEntrycount(PathManager.GetPathDirectoryInfo("Logs"));
                string _newlogpath = Path.Combine(PathManager.GetPathDirectoryInfo("Logs"), $"Log-{logentry_count++}");
                CurrentLogStream = File.Create(_newlogpath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
