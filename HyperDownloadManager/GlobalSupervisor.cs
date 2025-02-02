using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Dialogs;
using System.Windows;
using HyperDownloadManager.Views;
using HyperDownloadManager.ViewModels.Settings;
using HyperDownloadManager.Views.Pages.Setting;
using HyperDownloadManager.ViewModels;
using HyperDownloadManager.Log;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Download;

namespace HyperDownloadManager
{
    public static class GlobalSupervisor
    {
        public static bool UnderDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif

            }
        }
        public static Downloads? DownloadPage { get; set; }
        public static Settings? SettingsPage { get; set; }
        public static DialogBox? CurrentShowingDialog { get; set; }
        public static Logs? Logpage { get; set; }
        public static Info? InfoPage { get; set; }
        public static MainWindow? mainwindow { get; set; }
        public static GeneralSettingsViewModel? GeneralSettingsViewModel { get { return SettingSupervisor.GeneralSettings; } }
        public static NetworkSettingsViewModel? NetworkSettingsViewModel { get { return SettingSupervisor.NetworkSetting; } }
        public static ThemeSettingsViewModel? ThemeSettingsViewModel { get { return SettingSupervisor.ThemeSetting; } }
        public static GeneralSettingsView? GeneralSettingPage { get; set; }
        public static NetworkSettingsView? NetworkSettingPage { get; set; }
        public static ThemeSettingsView? ThemeSettingsPage { get; set; }
        public static MainViewModel? MainViewModel { get; set; } = new MainViewModel();

        private static Random Random = new Random();
        public static int GetRandom(int max, int min = 0)
        {
            return Random.Next(min, max);
        }
        public static void InitializeApplicationContent()
        {
            try
            {
                PathManager.CreateDirs();
                Logpage = new Logs();
                LogManager.Load_Logs(PathManager.GetPathDirectoryInfo("Logs"));
                SettingSupervisor.LoadSettings();
                InfoPage = new Info();
                ContainerManager.LoadContainers();
                DownloadManager.LoadDownloads();
                DownloadPage = new Downloads();
                NetworkSettingPage = new NetworkSettingsView(NetworkSettingsViewModel);
                GeneralSettingPage = new GeneralSettingsView(GeneralSettingsViewModel);
                ThemeSettingsPage = new ThemeSettingsView(ThemeSettingsViewModel);
                SettingsPage = new Settings();
                LogManager.Log(MessageLevel.Info, LogSection.Init, "Initialization Completed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"fail to initialize the app\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
        }
        public static bool OtherProcessExist()
        {
            try
            {
                string _name = Process.GetCurrentProcess().ProcessName;
                Process[] _p = Process.GetProcessesByName(_name);
                if (_p.Length > 1)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"fail to catch current process\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
