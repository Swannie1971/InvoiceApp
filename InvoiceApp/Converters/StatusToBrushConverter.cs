using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using InvoiceApp.Models;

namespace InvoiceApp.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InvoiceStatus status)
            {
                return status switch
                {
                    InvoiceStatus.Draft => new SolidColorBrush(Color.FromRgb(149, 165, 166)),      // Gray
                    InvoiceStatus.Sent => new SolidColorBrush(Color.FromRgb(52, 152, 219)),        // Blue
                    InvoiceStatus.PartiallyPaid => new SolidColorBrush(Color.FromRgb(241, 196, 15)), // Yellow/Orange
                    InvoiceStatus.Paid => new SolidColorBrush(Color.FromRgb(46, 204, 113)),        // Green
                    InvoiceStatus.Overdue => new SolidColorBrush(Color.FromRgb(231, 76, 60)),      // Red
                    _ => new SolidColorBrush(Color.FromRgb(189, 195, 199))                         // Light Gray
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}