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
using HyperDownloadManager.ViewModels.Download;
using HyperDownloadManager.Views.Pages.Download;
using MahApps.Metro.Controls;

namespace HyperDownloadManager
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : MetroWindow
    {
        public DownloadViewModel model { get; private set; } = new DownloadViewModel();
        public DownloadWindow(DownloadViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException("DownloadViewModel was null");

            InitializeComponent();
            this.model = viewModel;
            if (this.model.DownloadName != null)
                this.Title = this.model.DownloadName;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if(this.model.DetailPage is DownloadDetailView)
                {
                    this.model.IsSeparateWindowOpen = false;
                    this.model.DetailPage.NewWindow.Visibility = Visibility.Visible;
                    this.model.DetailPage.BackToMain.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(MessageLevel.Warning, LogSection.Download, ex.Message, true);
            }
        }
    }
}
