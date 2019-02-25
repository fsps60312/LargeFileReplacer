using System;
using System.Windows;
using System.Windows.Controls;

namespace LargeFileReplacer2
{
    public static class Extensions
    {
        public static UIElement Set(this UIElement u, int row, int column)
        {
            Grid.SetRow(u, row);
            Grid.SetColumn(u, column);
            return u;
        }
        public static UIElement SetSpan(this UIElement u, int rowSpan, int columnSpan)
        {
            Grid.SetRowSpan(u, rowSpan);
            Grid.SetColumnSpan(u, columnSpan);
            return u;
        }
        public static Button Set(this Button button, Action<Button> action)
        {
            button.FontSize = 15;
            button.Margin = new Thickness(2, 0, 2, 0);
            button.Click += delegate { action(button); };
            return button;
        }
        public static CheckBox Set(this CheckBox chb, Action<bool> action)
        {
            chb.FontSize = 15;
            chb.Margin = new Thickness(2, 0, 2, 0);
            chb.Checked += delegate { action(true); };
            chb.Unchecked += delegate { action(false); };
            return chb;
        }
        public static TextBox Set(this TextBox txb, Action<string> action)
        {
            txb.FontSize = 30;
            txb.TextChanged += delegate { action(txb.Text); };
            return txb;
        }
        public static bool IsUserVisible(this FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.IntersectsWith(bounds);
        }
    }
}
