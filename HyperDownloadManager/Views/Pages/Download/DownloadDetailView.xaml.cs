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
        public int Id { get; private set; }
        public bool IsSeparateWindowOpen;

        private DownloadViewModel model = new DownloadViewModel();
        private TimeSpan sTime;
        private PlotModel Plot = new PlotModel();
        public DownloadDetailView(DownloadViewModel viewModel)
        {
            InitializeComponent();
            this.model = viewModel;
            this.Id = viewModel.Id;
            this.DataContext = model;

            Plot = CreatePlotModel(((SolidColorBrush)App.Current.Resources["SecondaryBrush"]).Color, ((SolidColorBrush)App.Current.Resources["AccentBrush"]).Color);
            speedChart.DataContext = Plot;
            speedChart.ActualController.UnbindMouseDown(OxyMouseButton.Left);

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
        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            GlobalSupervisor.MainWindow.mainFrame.Content = GlobalSupervisor.DownloadPage;
        }
        public void ChartUpdate()
        {
            if (GlobalSupervisor.GeneralSettingsViewModel.UseChart)
            {
                speedChart.Dispatcher.Invoke(new Action(() =>
                {
                    sTime = TimeSpan.FromSeconds(sTime.TotalSeconds + 1);
                    if (Plot.Series.Count != 0)
                    {
                        AreaSeries? speedPoints = (Plot.Series[0] as AreaSeries);
                        if (speedPoints != null)
                        {
                            float speed = UnitConverter.ToMb(this.model.CurrentSpeed.OriginData);
                            speedPoints.Points.Add(new DataPoint(sTime.TotalSeconds, speed));
                            Plot.DefaultYAxis.Zoom(0, 2 * speed);
                            Plot.InvalidatePlot(true);
                        }
                    }
                }));
            }
        }
        private void NewWindow_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow downloadWindow = new DownloadWindow(this.model);
            downloadWindow.MainFrame.Content = this;
            NewWindow.Visibility = Visibility.Collapsed;
            BackToMain.Visibility = Visibility.Collapsed;
            this.model.IsSeparateWindowOpen = true;
            GlobalSupervisor.MainWindow.mainFrame.Content = GlobalSupervisor.DownloadPage;
            this.model.DownloadWindow = downloadWindow;
            downloadWindow.Show();
        }

        private void DownloadSettings_Click(object sender, RoutedEventArgs e)
        {
            if(this.model.DownloadSettingsPage != null)
                DialogManager.ShowDialog("Download Settings", this.model.DownloadSettingsPage, DialogMode.InApp);
        }

        private void ResumeDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!this.model.IsWorking)
                DownloadManager.SetStart(this.model.Id);
        }

        private void PauseDownload_Click(object sender, RoutedEventArgs e)
        {
            if (this.model.CurrentState == DownloadState.Downloading || this.model.CurrentState == DownloadState.Verifying)
                DownloadManager.SetStop(this.model.Id);
        }

        private async void DeleteDownload_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogManager.ShowMessageBox("Are you sure to remove this download?", MessageLevel.Warning, ButtonOrder.YESNO) == MessageBoxStatus.YES)
            {
                DownloadManager.SetStop(this.model.Id);
                DownloadManager.Remove(this.model.Id);
                if (this.model.IsSeparateWindowOpen)
                {
                    this.model.DownloadWindow?.Close();
                }
                else
                {
                    GlobalSupervisor.MainWindow.mainFrame.Content = GlobalSupervisor.DownloadPage;
                }
            }
        }
    }
}
