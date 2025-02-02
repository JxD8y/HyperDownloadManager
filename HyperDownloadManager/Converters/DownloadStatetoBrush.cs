using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HyperDownloadManager.ViewModels.Download;

namespace HyperDownloadManager.Converters
{
    public class DownloadStatetoBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is DownloadState)
                {
                    DownloadState _ds = (DownloadState)value;
                    if (_ds == DownloadState.Paused)
                    {
                        return new SolidColorBrush(Colors.Orange);
                    }
                    else if (_ds == DownloadState.Downloading)
                    {
                        return new SolidColorBrush(Colors.SeaGreen);
                    }
                    else if (_ds == DownloadState.Completed)
                    {
                        return new SolidColorBrush(Colors.RoyalBlue);
                    }
                    else if (_ds == DownloadState.AwaitingOnCondition)
                    {
                        return (SolidColorBrush)Application.Current.Resources["AccentBrush"];
                    }
                    else if (_ds == DownloadState.Verifying)
                    {
                        return new SolidColorBrush((Color)Application.Current.Resources["VColor"]);
                    }
                    return new SolidColorBrush(Colors.IndianRed);
                }
                else
                {
                    return (SolidColorBrush)Application.Current.Resources["AccentBrush"];
                }
            }
            return (SolidColorBrush)Application.Current.Resources["AccentBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
