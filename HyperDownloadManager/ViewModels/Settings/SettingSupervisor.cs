using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Repository;
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Proxy;
using LiteDB;
using Microsoft.Win32;

namespace HyperDownloadManager.ViewModels.Settings
{
    public static class SettingSupervisor
    {
        private static int clipBoardMaxSize = 0;
        public static GeneralSettingsViewModel? GeneralSettings { get; set; } = new GeneralSettingsViewModel();
        public static NetworkSettingsViewModel? NetworkSetting { get; set; } = new NetworkSettingsViewModel();
        public static ThemeSettingsViewModel? ThemeSetting { get; set; } = new ThemeSettingsViewModel();
        public static event EventHandler<EventArgs>? OnSettingsChanged;
        #region SettingTasks
        public static void SetStartupState(bool state)
        {
            try
            {
                RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                using (registryKey)
                {
                    if (state)
                    {
                        string path = System.Windows.Forms.Application.ExecutablePath;
                        registryKey?.SetValue("DeepDownload", path);
                    }
                    else
                    {
                        if (registryKey.GetValue("DeepDownload") != null)
                        {
                            registryKey?.DeleteValue("DeepDownload");
                        }
                    }
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Download, $"Fail to set startup state: {ex.Message}", true); }
        }
        public static void AutoCheckClipboard(int maxSize, bool state)
        {

        }
        public static void RestoreDefaultNetworkSettings()
        {
            NetworkSetting = new NetworkSettingsViewModel();
            NetworkSetting.BufferSize = 2042;
            NetworkSetting.MaxConnectionsPreServer = 1;
            NetworkSetting.EnsureSiteReturn200 = true;
            NetworkSetting.ResumeAfterError = true;
            NetworkSetting.MaxBytePreSecond = 1;
            NetworkSetting.DefaultHeaders = CoreFactory.NecessaryHeaders.FirstOrDefault();
            NetworkSetting.ProxyViewModel = new ProxyViewModel();
            NetworkSetting.Id = new BsonValue(Guid.NewGuid());
        }
        public static void RestoreDefaultThemeSettings()
        {
            ThemeSetting = new ThemeSettingsViewModel();
            ThemeSetting.DarkMode = true;
            ThemeSetting.Id = new BsonValue(Guid.NewGuid());
        }
        #endregion
        #region SettingRepo
        public static void LoadSettings()
        {
            LoadGeneralSettings();
            LoadNetSettings();
            LoadThemeSettings();
        }
        #region GeneralSettings
        public static void LoadGeneralSettings()
        {
            var List = LitedbRepo<GeneralSettingsViewModel>.Get(LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
            try
            {
                if (List.Count > 0)
                {
                    GeneralSettings = List.FirstOrDefault();
                }
                else
                {
                    SaveGeneralSettings(new GeneralSettingsViewModel());
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to load GeneralSettings: {ex.Message}");
            }
        }
        public static void SaveGeneralSettings(GeneralSettingsViewModel viewModel)
        {
            try
            {
                if (viewModel != null)
                {
                    var generalSettings = LitedbRepo<GeneralSettingsViewModel>.Get(LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
                    if (generalSettings.Count == 0)
                    {
                        BsonValue? id = LitedbRepo<GeneralSettingsViewModel>.Add(viewModel, LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
                    }
                    else
                    {
                        LitedbRepo<GeneralSettingsViewModel>.Update(GeneralSettings.Id, viewModel, LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
                    }
                    GeneralSettings = viewModel;
                    if (OnSettingsChanged != null)
                        OnSettingsChanged(viewModel, null);
                }
                else { SaveGeneralSettings(new GeneralSettingsViewModel()); }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to save GeneralSettings: {ex.Message}");
            }
        }
        #endregion
        #region NetSettingLoader
        public static void LoadNetSettings()
        {
            List<NetworkSettingsViewModel> _gs = LitedbRepo<NetworkSettingsViewModel>.Get(LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
            try
            {
                if (_gs.Count() > 0)
                {
                    NetworkSetting = _gs.First();
                }
                else
                {
                    RestoreDefaultNetworkSettings();
                    SaveNetSettings();
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Load NetSettings: {ex.Message}"); }
        }
        public static void SaveNetSettings()
        {
            if (NetworkSetting != null)
            {
                try
                {
                    var netsetting = LitedbRepo<NetworkSettingsViewModel>.Get(LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
                    if (netsetting.Count == 0)
                    {
                        LiteDB.BsonValue id = LitedbRepo<NetworkSettingsViewModel>.Add(NetworkSetting, LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
                    }
                    else
                    {
                        LitedbRepo<NetworkSettingsViewModel>.Update(NetworkSetting.Id, NetworkSetting, LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
                    }
                }
                catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Save NetSettings: {ex.Message}"); }
            }
            else { LoadNetSettings(); SaveNetSettings(); }
        }
        #endregion
        #region ThemeSettingLoader
        public static void LoadThemeSettings()
        {
            List<ThemeSettingsViewModel> _gs = LitedbRepo<ThemeSettingsViewModel>.Get(LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
            try
            {
                if (_gs.Count() > 0)
                {
                    ThemeSetting.DarkMode = _gs.First().DarkMode;
                    ThemeSetting.Id = _gs.First().Id;
                    if (ThemeSetting.DarkMode)
                    {
                        ThemeSetting.ToDark(true);
                    }
                    else
                    {
                        ThemeSetting.ToLight(true);
                    }
                }
                else
                {
                    RestoreDefaultThemeSettings();
                    SaveThemeSettings();
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Load ThemeSettings: {ex.Message}"); }
        }
        public static void SaveThemeSettings()
        {
            if (ThemeSetting != null)
            {
                try
                {
                    var themesetting = LitedbRepo<ThemeSettingsViewModel>.Get(LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
                    if (themesetting.Count == 0)
                    {
                        LiteDB.BsonValue id = LitedbRepo<ThemeSettingsViewModel>.Add(ThemeSetting, LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
                    }
                    else
                    {
                        LitedbRepo<ThemeSettingsViewModel>.Update(ThemeSetting.Id, ThemeSetting, LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
                    }
                }
                catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Save ThemeSettings: {ex.Message}"); }
            }
            else { LoadThemeSettings(); SaveThemeSettings(); }
        }
        #endregion
        #endregion
    }
}
