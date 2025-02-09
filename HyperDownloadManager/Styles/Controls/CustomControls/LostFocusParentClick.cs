using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace HyperDownloadManager.Styles.Controls.CustomControls
{
    public class LostFocusParentClick
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(LostFocusParentClick), new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(EnabledProperty, value);
        }

        private static void OnEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var parentControl = obj as UIElement;
            if (parentControl != null)
            {
                if ((bool)e.NewValue)
                {
                    parentControl.MouseLeftButtonDown += ParentControl_MouseLeftButtonDown;
                }
                else
                {
                    parentControl.MouseLeftButtonDown -= ParentControl_MouseLeftButtonDown;
                }
            }
        }

        private static void ParentControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentControl = sender as UIElement;
            if (parentControl != null)
            {
                parentControl.Focus();
                e.Handled = true;
            }
        }
    }
}
