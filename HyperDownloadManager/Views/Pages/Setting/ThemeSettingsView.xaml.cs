using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HyperDownloadManager.ViewModels.Settings;

namespace HyperDownloadManager.Views.Pages.Setting
{
    /// <summary>
    /// Interaction logic for ThemeSettingsView.xaml
    /// </summary>
    public partial class ThemeSettingsView : Page
    {
        ThemeSettingsViewModel SettingView = null;
        public ThemeSettingsView(ThemeSettingsViewModel model)
        {
            InitializeComponent();
            SettingView = model;
            DataContext = SettingView;
        }
        private void night_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SettingView.ToDark();
        }

        private void day_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SettingView.ToLight();
        }
    }
}
