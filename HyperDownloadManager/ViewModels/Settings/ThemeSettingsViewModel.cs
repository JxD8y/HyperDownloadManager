using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using HyperDownloadManager.Dialogs;
using LiteDB;

namespace HyperDownloadManager.ViewModels.Settings
{
    public class ThemeSettingsViewModel : ViewModel
    {

        private HDMTheme theme = HDMTheme.Dark;
        public ThemeSettingsViewModel()
        {
            this.ThemeMode = HDMTheme.Dark;
            this.Id = new BsonValue(Guid.NewGuid());
        }

        [BsonIgnore]
        public static Theme LightTheme = new Theme("CustomLight", "CustomLight", "Light", "White", (Color)Application.Current.Resources["WBackgroundColor"], (Brush)Application.Current.Resources["BackgroundBrush"], true, false);
        [BsonIgnore]
        public static Theme DarkTheme = new Theme("CustomDark", "CustomDark", "Dark", "Black", (Color)Application.Current.Resources["DObsoleteColor"], (Brush)Application.Current.Resources["DObsoleteBrush"], true, false);
        public HDMTheme ThemeMode { get { return theme; } set { theme = value; OnPropertyChanged(); } }
        [BsonIgnore]
        public Theme CurrentTheme { get; set; } = DarkTheme;
        [BsonId]
        public BsonValue? Id { get; set; }

        public async void ApplyTheme()
        {
            try
            {
                ResourceDictionary themeResource = new ResourceDictionary();
                switch (this.ThemeMode)
                {
                    case HDMTheme.Dark:
                        themeResource.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                        this.CurrentTheme = DarkTheme;
                        break;
                    case HDMTheme.Light:
                        themeResource.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                        this.CurrentTheme = LightTheme;
                        break;
                }
                Application.Current.Resources.MergedDictionaries.Add(themeResource);
                
                ThemeManager.Current.ChangeTheme(Application.Current, GlobalSupervisor.ThemeSettingsViewModel.CurrentTheme);
            }
            catch (Exception ex)
            {
                await DialogManager.ShowMessageBox($"Cannot change theme: {ex.Message}", Dialogs.MessageBoxDialog.MessageLevel.Error, Dialogs.MessageBoxDialog.ButtonOrder.OK, true);
            }
        }
    }
}
