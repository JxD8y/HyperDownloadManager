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
using HyperDownloadManager.Utils;
using HyperDownloadManager.ViewModels.Download;

namespace HyperDownloadManager.Dialogs.DownloadDialog
{
    /// <summary>
    /// Interaction logic for DownloadFinishedDialog.xaml
    /// </summary>
    public partial class DownloadFinishedDialog : Page
    {
        DownloadViewModel model = new DownloadViewModel();
        public DownloadFinishedDialog(DownloadViewModel viewModel)
        {
            this.model = viewModel;
            InitializeComponent();
            this.path.Text = model.FileSavePath ?? "";
            this.Url.Text = model.CurrentUrl;
            this.FileName.Content = IOUtility.AdjustFileNameString(model.CurrentFileName ?? "");
            this.FileSize.Content = model.FileSize.DataValue.ToString("0.00");
            this.filesizeunit.Content = model.FileSize.DataUnit;
            this.FileIcon.Source = model.Icon;
        }
        private void openFile_Click(object sender, RoutedEventArgs e)
        {
            IOUtility.OpenExplorer(model.FileSavePath);
        }
        private void openFolder_Click(object sender, RoutedEventArgs e)
        {
            IOUtility.OpenExplorer(model.FileSavePath);
        }
    }
}
