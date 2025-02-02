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
        DownloadViewModel _downloadViewModel;
        public DownloadFinishedDialog(DownloadViewModel model)
        {
            _downloadViewModel = model;
            InitializeComponent();
            this.path.Text = model.IOCore.fileSavePath;
            this.Url.Text = model.IOCore.Url.OriginalString;
            //this.FileName.Content = model.AdjustFileNameString(model.IOCore.fileName); N: where is function?
            this.FileSize.Content = model.IOCore.fileSize.DataValue.ToString("0.00");
            this.filesizeunit.Content = model.IOCore.fileSize.DataUnit;
            this.FileIcon.Source = model.Icon;
        }
        private void openFile_Click(object sender, RoutedEventArgs e)
        {
            IOUtility.OpenExplorer(_downloadViewModel.IOCore.fileSavePath);
        }
        private void openFolder_Click(object sender, RoutedEventArgs e)
        {
            IOUtility.OpenExplorer(_downloadViewModel.IOCore.saveDirectory);
        }
    }
}
