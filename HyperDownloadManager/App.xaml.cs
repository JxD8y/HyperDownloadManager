using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace HyperDownloadManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!GlobalSupervisor.OtherProcessExist())
            {
                GlobalSupervisor.InitializeApplicationContent();
            }
            else
            {
                MessageBox.Show("Other Process Exist Exiting...", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Process.GetCurrentProcess().Kill();
            }
        }
    }

}
