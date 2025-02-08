using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using HyperDownloadManager.Views.Pages.Setting;

namespace HyperDownloadManager.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            this.settingFrame.Content = GlobalSupervisor.GeneralSettingPage;
        }
        public void UpdateLabelsColor()
        {
            foreach (var child in Toppanel_stack.Children)
            {
                if(child is Label label)
                    label.Foreground = (SolidColorBrush)App.Current.Resources["ForegroundBrush"];
            }
        }
        public void Settings_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Label sndLabel = (Label)sender;
                foreach (var child in Toppanel_stack.Children)
                {
                    if (child is Label label)
                        label.Foreground = (SolidColorBrush)App.Current.Resources["ForegroundBrush"];
                }
                sndLabel.Foreground = (SolidColorBrush)App.Current.Resources["PrimaryBrush"];
                switch (sndLabel.Name)
                {
                    case "General":
                        settingFrame.Content = GlobalSupervisor.GeneralSettingPage;
                        ControlPanel.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "Theme":
                        settingFrame.Content = GlobalSupervisor.ThemeSettingsPage;
                        ControlPanel.Visibility = System.Windows.Visibility.Collapsed;
                        break;
                    case "Network":
                        settingFrame.Content = GlobalSupervisor.NetworkSettingPage;
                        ControlPanel.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }
            catch
            {
                LogManager.Log(MessageLevel.Warning, LogSection.Setting, $"Cannot switch pages");
            }
        }

        private void RestoreButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (settingFrame.Content is GeneralSettingsView)
                GlobalSupervisor.GeneralSettingPage?.Restore();
            if (settingFrame.Content is NetworkSettingsView)
                GlobalSupervisor.NetworkSettingPage?.Restore();
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (settingFrame.Content is GeneralSettingsView)
                GlobalSupervisor.GeneralSettingPage?.Save();
            if (settingFrame.Content is NetworkSettingsView)
                GlobalSupervisor.NetworkSettingPage?.Save();
        }
    }
}
