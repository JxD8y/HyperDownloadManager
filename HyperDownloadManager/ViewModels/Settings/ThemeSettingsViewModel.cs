using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Settings
{
    public class ThemeSettingsViewModel : ViewModel
    {
        private bool _dark = true;

        [BsonIgnore]
        public static Theme LightTheme = new Theme("CustomLight", "CustomLight", "Light", "White", (Color)Application.Current.Resources["WBackgroundColor"], (Brush)Application.Current.Resources["BackgroundBrush"], true, false);
        [BsonIgnore]
        public static Theme DarkTheme = new Theme("CustomDark", "CustomDark", "Dark", "Black", (Color)Application.Current.Resources["DObsoleteColor"], (Brush)Application.Current.Resources["DObsoleteBrush"], true, false);
        public bool DarkMode { get { return _dark; } set { _dark = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public Theme CurrentTheme { get; set; } = DarkTheme;
        [BsonId]
        public BsonValue Id { get; set; }

        public void ToDark(bool warmup = false) //Theme switch is changed !
        {
            Application.Current.Resources["BorderBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DBorderColor"]);
            Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DBackgroundColor"]);
            Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DForegroundColor"]);
            Application.Current.Resources["SecondaryBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DSecondryColor"]);
            Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DPrimaryColor"]);
            Application.Current.Resources["BoxBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DBoxColor"]);
            Application.Current.Resources["ObsoleteBrush"] = new SolidColorBrush((Color)Application.Current.Resources["DObsoleteColor"]);
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)Application.Current.Resources["AccentColor"]);
            DarkMode = true;
            CurrentTheme = DarkTheme;
            if (!warmup)
            {
                ThemeManager.Current.ChangeTheme(Application.Current, CurrentTheme);
                SettingSupervisor.SaveThemeSettings();
                GlobalSupervisor.SettingsPage.UpdateLabelsColor();
            }
        }
        public void ToLight(bool warmup = false)
        {
            Application.Current.Resources["BorderBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WBorderColor"]);
            Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WBackgroundColor"]);
            Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WForegroundColor"]);
            Application.Current.Resources["SecondaryBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WSecondryColor"]);
            Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WPrimaryColor"]);
            Application.Current.Resources["BoxBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WBoxColor"]);
            Application.Current.Resources["ObsoleteBrush"] = new SolidColorBrush((Color)Application.Current.Resources["WObsoleteColor"]);
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush((Color)Application.Current.Resources["AccentColor"]);
            DarkMode = false;
            CurrentTheme = LightTheme;
            if (!warmup)
            {
                ThemeManager.Current.ChangeTheme(Application.Current, CurrentTheme);
                SettingSupervisor.SaveThemeSettings();
                GlobalSupervisor.SettingsPage.UpdateLabelsColor();
            }
        }
    }
}
