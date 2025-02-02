using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Log;

namespace HyperDownloadManager.Views
{
    /// <summary>
    /// Interaction logic for Logs.xaml
    /// </summary>
    public partial class Logs : Page
    {
        public Logs()
        {
            LogManager.OnLogAdd += Logviewer_Updater;
            InitializeComponent();
        }
        public void Logviewer_Updater(LogViewModel lv)
        {
            try
            {
                this.Dispatcher.Invoke(() => { Loglist.Items.Add(lv); });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private async void Deletecurrentlog_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Do you want to clear all logs?", MessageLevel.Warning, ButtonOrder.YESNO, true) == MessageBoxStatus.YES)
            {
                if (LogManager.Logs.Count == 0)
                {
                    await DialogManager.ShowMessageBox("No log to remove.", MessageLevel.Info, ButtonOrder.OK, true);
                }
                else
                {
                    LogManager.Logs = new List<LogViewModel>();
                    try { LogManager.CurrentLogStream.Close(); File.Delete(LogManager.CurrentLogFullname); } catch { System.Windows.MessageBox.Show("Fail to Remove Log File", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                    Loglist.Items.Clear();
                    if (!LogManager.CreateNewLogEntry()) { await DialogManager.ShowMessageBox("All logs cleared", MessageLevel.Info, ButtonOrder.OK, true); return; }
                    LogManager.Log(MessageLevel.Warning, LogSection.Log, "All logs cleared!");
                }
            }
        }
        private void openexplorerlog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", $"/select, {LogManager.CurrentLogFullname}");
        }

        private async void Savelogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.Log(MessageLevel.Info, LogSection.Log, "Starting to export log");
                SaveFileDialog _ps = new SaveFileDialog();
                _ps.Title = "Save Logs into File";
                if (_ps.ShowDialog() == DialogResult.OK)
                {
                    FileStream fs = File.Open(_ps.FileName, FileMode.OpenOrCreate);
                    foreach (LogViewModel lvm in LogManager.Logs)
                    {
                        string logline = $"{lvm.logLevel},{lvm.logsection},{lvm.AccureTime},{lvm.Log}\n";
                        byte[] logline_b = Encoding.ASCII.GetBytes(logline);
                        fs.Write(logline_b, 0, logline_b.Length);
                    }
                    fs.Flush();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                await DialogManager.ShowMessageBox($"LogSave Error:\n{ex.Message}", MessageLevel.Info, ButtonOrder.OK, true);
            }
        }
    }
}
