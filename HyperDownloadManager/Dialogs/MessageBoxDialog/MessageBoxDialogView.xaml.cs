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

namespace HyperDownloadManager.Dialogs.MessageBoxDialog
{
    /// <summary>
    /// Interaction logic for MessageBoxDialogView.xaml
    /// </summary>
    public partial class MessageBoxDialogView : Page
    {
        public string Message { get; set; }
        public MessageLevel Level { get; set; }
        public ButtonOrder ButtonOrder { get; set; }
        public DialogViewModel DialogViewModel { get; set; }
        public MessageBoxDialogView(string message, MessageLevel level, ButtonOrder buttonOrder, DialogViewModel dialogView)
        {
            InitializeComponent();
            this.DialogViewModel = dialogView;
            Message = message;
            Level = level;
            ButtonOrder = buttonOrder;
            switch (ButtonOrder)
            {
                case ButtonOrder.OK:
                    OK.Visibility = Visibility.Visible;
                    break;
                case ButtonOrder.OKCANCLE:
                    OKCANCEL.Visibility = Visibility.Visible;
                    break;
                case ButtonOrder.OKCANCLEIGNORE:
                    OKCANCLEIGNORE.Visibility = Visibility.Visible;
                    break;
                case ButtonOrder.YESNO:
                    YESNO.Visibility = Visibility.Visible;
                    break;
                case ButtonOrder.CONTINUEABORT:
                    CONTINUEABORT.Visibility = Visibility.Visible;
                    break;
                default:
                    OK.Visibility = Visibility.Visible;
                    break;
            }
            switch (Level)
            {
                case MessageLevel.Info:
                    InfoIcon.Visibility = Visibility.Visible;
                    TitleLabel.Content = "Information";
                    MainBorder.BorderBrush = new SolidColorBrush(Colors.RoyalBlue);
                    break;
                case MessageLevel.Warning:
                    WarnIcon.Visibility = Visibility.Visible;
                    TitleLabel.Content = "Warning";
                    MainBorder.BorderBrush = new SolidColorBrush(Colors.Orange);
                    break;
                case MessageLevel.Error:
                    ErrorIcon.Visibility = Visibility.Visible;
                    TitleLabel.Content = "Error";
                    MainBorder.BorderBrush = new SolidColorBrush(Colors.IndianRed);
                    break;
                default:
                    ErrorIcon.Visibility = Visibility.Visible;
                    TitleLabel.Content = "Error";
                    MainBorder.BorderBrush = new SolidColorBrush(Colors.IndianRed);
                    break;
            }
            ContentLabel.Text = message;
            if (dialogView.DialogMode == DialogMode.Window)
            {
                this.MainBorder.CornerRadius = new CornerRadius(0);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.OK);
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.IGNORE);
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.YES);
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.NO);
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.CONTINUE);
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.ABORT);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogViewModel.Close(MessageBoxStatus.CANCEL);
        }
    }
}
