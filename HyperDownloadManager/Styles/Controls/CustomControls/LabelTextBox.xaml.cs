using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace HyperDownloadManager.Styles.Controls.CustomControls
{
    /// <summary>
    /// Interaction logic for LabelTextBox.xaml
    /// </summary>
    public partial class LabelTextBox : UserControl,INotifyPropertyChanged
    {
        public static readonly DependencyProperty IsReadonlyProperty =
       DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(LabelTextBox), new PropertyMetadata(false));
        public static readonly DependencyProperty IsNumericalInputProperty =
       DependencyProperty.Register("IsNumericalInput", typeof(bool), typeof(LabelTextBox), new PropertyMetadata(false));
        public static readonly DependencyProperty MinimumProperty =
       DependencyProperty.Register("Minimum", typeof(int), typeof(LabelTextBox), new PropertyMetadata(0));
        public static readonly DependencyProperty MaximumProperty =
       DependencyProperty.Register("Maximum", typeof(int), typeof(LabelTextBox), new PropertyMetadata(int.MaxValue));
        public static readonly DependencyProperty HelperContentProperty =
       DependencyProperty.Register("HelperContent", typeof(object), typeof(LabelTextBox), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TextProperty =
      DependencyProperty.Register("Text", typeof(string), typeof(LabelTextBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object HelperContent
        {
            get { return (object)GetValue(HelperContentProperty); }
            set { SetValue(HelperContentProperty, value); }
        }
        public bool IsNumericalInput
        {
            get { return (bool)GetValue(IsNumericalInputProperty); }
            set { SetValue(IsNumericalInputProperty, value); }
        }
        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(IsNumericalInputProperty, value); }
        }
        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(IsNumericalInputProperty, value); }
        }
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadonlyProperty); }
            set { SetValue(IsReadonlyProperty, value); }
        }
        public string Text
        {
            get { return textbox.Text; }
            set { SetValue(TextProperty, value); OnPropertyChanged(); }
        }
        public LabelTextBox()
        {
            InitializeComponent();
            this.textbox.DataContext = this;
        }
        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsNumericalInput)
            {
                if (textbox.Text.Length != 0 && !char.IsDigit(textbox.Text.Last()))
                {
                    textbox.Text = textbox.Text.Remove(textbox.Text.Length - 1);
                }
                int num;
                if (Int32.TryParse(textbox.Text, out num))
                {
                    if (num > Maximum)
                    {
                        textbox.Text = Maximum.ToString();
                    }
                    else if (num < Minimum)
                    {
                        textbox.Text = Minimum.ToString();
                    }
                }
                else
                {
                    textbox.Text = "";
                }
            }
        }
    }
}
