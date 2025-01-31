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
using System.Windows.Shapes;
using ControlzEx.Theming;
using MahApps.Metro.Controls;

namespace HyperDownloadManager.Dialogs
{
    /// <summary>
    /// Interaction logic for DialogBox.xaml
    /// </summary>
    public partial class DialogBox : MetroWindow
    {
        public DialogBox(string title)
        {
            InitializeComponent();
            this.Title = title;
            if (GlobalSupervisor.ThemeSettingsViewModel == null)
                ThemeManager.Current.ChangeTheme(Application.Current, ThemeSettingsViewModel.DarkTheme);
            else
                ThemeManager.Current.ChangeTheme(Application.Current, GlobalSupervisor.ThemeSettingsViewModel.CurrentTheme);
        }
    }
}
