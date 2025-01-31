using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ControlzEx.Theming;
using MahApps.Metro.Controls;

namespace HyperDownloadManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Theme DarkTheme = new Theme("CustomDark", "CustomDark", "Dark", "Black", (Color)Application.Current.Resources["DObsoleteColor"], (Brush)Application.Current.Resources["DObsoleteBrush"], true, false);
            ThemeManager.Current.ChangeTheme(Application.Current, DarkTheme);
        }
    }
}