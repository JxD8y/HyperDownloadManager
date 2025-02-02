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
using HyperDownloadManager.Dialogs.MessageBoxDialog;
using HyperDownloadManager.Dialogs;
using HyperDownloadManager.ViewModels.DataUnit;
using HyperDownloadManager.ViewModels.Download;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace HyperDownloadManager.Views.Pages.Download
{
    /// <summary>
    /// Interaction logic for DownloadDetailView.xaml
    /// </summary>
    public partial class DownloadDetailView : Page
    {
        public int Id { get; set; }
        public bool IsSeparateWindowOpen;

        private DownloadViewModel downloadViewModel;
        private TimeSpan Stime;
        private PlotModel Plot = new PlotModel();
        public DownloadDetailView(int id)
        {
            Id = id;
            InitializeComponent();
            Plot = CreatePlotModel(((SolidColorBrush)App.Current.Resources["SecondaryBrush"]).Color, ((SolidColorBrush)App.Current.Resources["AccentBrush"]).Color);
            speedChart.DataContext = Plot;
            Plot.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == OxyMouseButton.Left)
                {
                    speedChart.ActualController.UnbindMouseDown(OxyMouseButton.Left);
                }
            };
            if (GlobalSupervisor.GeneralSettingsViewModel.UseChart)
                speedChart.Visibility = Visibility.Visible;
            else
                speedChart.Visibility = Visibility.Collapsed;
        }
        private PlotModel CreatePlotModel(Color SecondaryBrush, Color AccentColor)
        {
            PlotModel plotModel = new PlotModel();
            OxyColor BorderColor = SecondaryBrush.ToOxyColor();
            plotModel.TextColor = OxyColors.White;
            plotModel.Padding = new OxyThickness(10);
            plotModel.PlotMargins = new OxyThickness(30);
            plotModel.PlotAreaBorderColor = OxyColors.Transparent;
            TimeSpanAxis XAxis = new TimeSpanAxis()
            {
                Position = AxisPosition.Bottom,
                TicklineColor = BorderColor,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = BorderColor,
                MinorGridlineColor = BorderColor,
                Key = "XA",

            };
            LinearAxis YAxis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                TicklineColor = BorderColor,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = BorderColor,
                MinorGridlineColor = BorderColor,
                Angle = 90,
                Maximum = double.NaN,
                Minimum = double.NaN,
                Key = "YA",
                Unit = "Mb/s"
            };
            plotModel.Axes.Clear();
            plotModel.Series.Clear();
            plotModel.Axes.Add(XAxis);
            plotModel.Axes.Add(YAxis);
            plotModel.Series.Add(new AreaSeries()
            {
                Color = AccentColor.ToOxyColor(),
                MarkerType = MarkerType.None,
                MarkerStroke = OxyColors.White,
                BrokenLineColor = OxyColors.White,
                XAxisKey = XAxis.Key,
                YAxisKey = YAxis.Key,
                TrackerFormatString = ""
            });
            return plotModel;
        }
        private void Backtomain_Click(object sender, RoutedEventArgs e)
        {
            GlobalSupervisor.mainwindow.mainFrame.Content = GlobalSupervisor.DownloadPage;
        }

        public void SetdataContext(DownloadViewModel downloadViewModel)
        {
            this.DataContext = downloadViewModel;
            this.downloadViewModel = downloadViewModel;
        }
        private void Ps_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DownloadManager.SetStop(Id);
        }

        private void Rn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DownloadManager.SetStart(Id);
        }
        public void ChartUpdate()
        {
            speedChart.Dispatcher.Invoke(new Action(() =>
            {
                Stime = TimeSpan.FromSeconds(Stime.TotalSeconds + 1);
                if (Plot.Series.Count != 0)
                {
                    AreaSeries? speedPoints = (Plot.Series[0] as AreaSeries);
                    if (speedPoints != null)
                    {
                        float speed = UnitConverter.ToMb(downloadViewModel.Current_Speed.OriginData);
                        speedPoints.Points.Add(new DataPoint(Stime.TotalSeconds, speed));
                        Plot.DefaultYAxis.Zoom(0, 2 * speed);
                        Plot.InvalidatePlot(true);
                    }
                }
            }));
        }
        private void NewWindow_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow sp = new DownloadWindow((this.DataContext as DownloadViewModel).Current_FileName);
            sp.MainFrame.Content = this;
            NewWindow.Visibility = Visibility.Collapsed;
            Backtomain.Visibility = Visibility.Collapsed;
            (this.DataContext as DownloadViewModel).IsSeparateWindowOpen = true;
            GlobalSupervisor.mainwindow.mainFrame.Content = GlobalSupervisor.DownloadPage;
            (this.DataContext as DownloadViewModel).DownloadWindow = sp;
            sp.Show();
        }

        private void DownloadSettings_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.ShowDialog("Download Settings", downloadViewModel.DownloadSettingsPage, DialogMode.InApp);
        }

        private void ResumeDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!downloadViewModel.Supervisor.IsWorking)
                DownloadManager.SetStart(downloadViewModel.Id);
        }

        private void PauseDownload_Click(object sender, RoutedEventArgs e)
        {
            if (downloadViewModel.Current_State == DownloadState.Downloading || downloadViewModel.Current_State == DownloadState.Verifying)
                DownloadManager.SetStop(downloadViewModel.Id);
        }

        private async void DeleteDownload_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("are you sure to Remove this Download", MessageLevel.Warning, ButtonOrder.YESNO) == MessageBoxStatus.YES)
            {
                DownloadManager.SetStop(downloadViewModel.Id);
                DownloadManager.Remove(downloadViewModel.Id);
                if (downloadViewModel.IsSeparateWindowOpen)
                {
                    downloadViewModel.DownloadWindow.Close();
                }
                else
                {
                    GlobalSupervisor.mainwindow.mainFrame.Content = GlobalSupervisor.DownloadPage;
                }
            }
        }
    }
}
