using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.ViewModels;
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.ViewModels.Download.Container;
using HyperDownloadManager.ViewModels.Settings;
using HyperDownloadManager.Views;
using HyperDownloadManager.Views.Pages.Setting;

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
        public static MainViewModel MainViewModel { get; set; } = new MainViewModel();
        public static Downloads? DownloadPage { get; set; }
        public static Settings? SettingsPage { get; set; }
        public static DialogBox? CurrentShowingDialog { get; set; }
        public static Logs? LogPage { get; set; }
        public static Info? InfoPage { get; set; }
        public static MainWindow MainWindow { get; set; } = new MainWindow();
        public static GeneralSettingsViewModel GeneralSettingsViewModel { get { return SettingSupervisor.GeneralSettings; } }
        public static NetworkSettingsViewModel NetworkSettingsViewModel { get { return SettingSupervisor.NetworkSetting; } }
        public static ThemeSettingsViewModel ThemeSettingsViewModel { get { return SettingSupervisor.ThemeSetting; } }
        public static GeneralSettingsView GeneralSettingPage { get; set; } = new GeneralSettingsView(GeneralSettingsViewModel);
        public static NetworkSettingsView NetworkSettingPage { get; set; } = new NetworkSettingsView(NetworkSettingsViewModel);
        public static ThemeSettingsView ThemeSettingsPage { get; set; } = new ThemeSettingsView(ThemeSettingsViewModel);

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
                LogPage = new Logs();
                LogManager.Load_Logs(PathManager.GetPathDirectoryInfo("Logs"));
                SettingSupervisor.LoadSettings();
                ThemeSettingsPage = new ThemeSettingsView(SettingSupervisor.ThemeSetting);
                GeneralSettingPage = new GeneralSettingsView(SettingSupervisor.GeneralSettings);
                NetworkSettingPage = new NetworkSettingsView(SettingSupervisor.NetworkSetting);
                InfoPage = new Info();
                ContainerManager.LoadContainers();
                DownloadManager.LoadDownloads();
                DownloadPage = new Downloads();
                SettingsPage = new Settings();
                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                LogManager.Log(MessageLevel.Info, LogSection.Init, "Initialization Completed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"fail to initialize the app\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogManager.Log(MessageLevel.Error, LogSection.Download, e.Exception.Message);
            e.SetObserved();
        }

        private static void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogManager.Log(MessageLevel.Error, LogSection.Download, e.Exception.Message);
            e.Handled = true;
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
