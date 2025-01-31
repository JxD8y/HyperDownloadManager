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
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;
using MahApps.Metro.Controls;

namespace HyperDownloadManager
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : MetroWindow
    {
        public DownloadWindow(string title)
        {
            InitializeComponent();
            this.Title = title;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                (((DetailDownload)MainFrame.Content).DataContext as DownloadViewModel).IsSeparateWindowOpen = false;
                ((DetailDownload)MainFrame.Content).NewWindow.Visibility = Visibility.Visible;
                ((DetailDownload)MainFrame.Content).Backtomain.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Warning, LogManager.LogSection.Download, ex.Message, true);
            }
        }
    }
}
