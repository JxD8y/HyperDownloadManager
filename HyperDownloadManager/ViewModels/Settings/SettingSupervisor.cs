using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Repository;
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download.DownloadCore;
using HyperDownloadManager.ViewModels.Proxy;
using LiteDB;
using Microsoft.Win32;

namespace HyperDownloadManager.ViewModels.Settings
{
    public static class SettingSupervisor
    {
        public static GeneralSettingsViewModel GeneralSettings { get; set; } = new GeneralSettingsViewModel();
        public static NetworkSettingsViewModel NetworkSetting { get; set; } = new NetworkSettingsViewModel();
        public static ThemeSettingsViewModel ThemeSetting { get; set; } = new ThemeSettingsViewModel();

        public static event EventHandler<EventArgs?>? OnSettingsChanged;

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
                        registryKey?.SetValue("HyperDownloadManager", path);
                    }
                    else
                    {
                        if (registryKey?.GetValue("HyperDownloadManager") != null)
                        {
                            registryKey?.DeleteValue("HyperDownloadManager");
                        }
                    }
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Download, $"Fail to set startup state: {ex.Message}", true); }
        }
        public static void LoadSettings()
        {
            LoadGeneralSettings();
            LoadNetSettings();
            LoadThemeSettings();
        }
        public static void LoadGeneralSettings()
        {
            var List = LitedbRepo<GeneralSettingsViewModel>.Get(LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
            try
            {
                if (List.Count > 0)
                {
                    GeneralSettings = List[0];
                }
                else
                {
                    SaveGeneralSettings();
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to load GeneralSettings: {ex.Message}");
            }
        }
        public static void SaveGeneralSettings()
        {
            try
            {
                if (GeneralSettings == null)
                    GeneralSettings = new GeneralSettingsViewModel();

                var generalSettings = LitedbRepo<GeneralSettingsViewModel>.Get(LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
                if (generalSettings.Count == 0)
                {
                    LitedbRepo<GeneralSettingsViewModel>.Add(GeneralSettings, LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
                }
                else
                {
                    if (GeneralSettings.Id != null)
                        LitedbRepo<GeneralSettingsViewModel>.Update(GeneralSettings.Id, GeneralSettings, LitedbRepo<GeneralSettingsViewModel>.GeneralSettingsColName);
                    else
                        throw new Exception("General Setting id was null");
                }
                if (OnSettingsChanged != null)
                    OnSettingsChanged(GeneralSettings, null);
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to save GeneralSettings: {ex.Message}");
            }
        }
        public static void LoadNetSettings()
        {
            List<NetworkSettingsViewModel> list = LitedbRepo<NetworkSettingsViewModel>.Get(LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
            try
            {
                if (list.Count() > 0)
                {
                    NetworkSetting = list[0];
                }
                else
                {
                    SaveNetSettings();
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Load NetSettings: {ex.Message}"); }
        }
        public static void SaveNetSettings()
        {
            try
            {
                if (NetworkSetting == null)
                    NetworkSetting = new NetworkSettingsViewModel();

                var netSettings = LitedbRepo<NetworkSettingsViewModel>.Get(LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
                if (netSettings.Count == 0)
                {
                    LitedbRepo<NetworkSettingsViewModel>.Add(NetworkSetting, LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
                }
                else
                {
                    if(NetworkSetting.Id != null)
                        LitedbRepo<NetworkSettingsViewModel>.Update(NetworkSetting.Id, NetworkSetting, LitedbRepo<NetworkSettingsViewModel>.NetSettingsColName);
                    else
                        throw new Exception("Network Setting id was null");
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Save Network Settings: {ex.Message}"); }
        }
        public static void LoadThemeSettings()
        {
            List<ThemeSettingsViewModel> list = LitedbRepo<ThemeSettingsViewModel>.Get(LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
            try
            {
                if (list.Count() > 0)
                {
                    ThemeSetting = list[0];
                    ThemeSetting.ApplyTheme();
                }
                else
                {
                    SaveThemeSettings();
                }
            }
            catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Load ThemeSettings: {ex.Message}"); }
        }
        public static void SaveThemeSettings()
        {
                try
                {
                    if (ThemeSetting == null)
                        ThemeSetting = new ThemeSettingsViewModel();

                    var themeSetting = LitedbRepo<ThemeSettingsViewModel>.Get(LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
                    if (themeSetting.Count == 0)
                    {
                        LitedbRepo<ThemeSettingsViewModel>.Add(ThemeSetting, LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
                    }
                    else
                    {
                        if(ThemeSetting.Id != null)
                            LitedbRepo<ThemeSettingsViewModel>.Update(ThemeSetting.Id, ThemeSetting, LitedbRepo<ThemeSettingsViewModel>.ThemeSettingsColName);
                        else
                            throw new Exception("Theme Setting id was null");
                    }
                }
                catch (Exception ex) { LogManager.Log(MessageLevel.Error, LogSection.Setting, $"Fail to Save ThemeSettings: {ex.Message}"); }
        }
    }
}
