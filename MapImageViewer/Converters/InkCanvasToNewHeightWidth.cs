using System;
using Windows.UI.Xaml.Data;

namespace MapImageViewer.Converters
{
    public class InkCanvasToNewHeightWidth : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, string language)
        {
            return (double)value * 12;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}