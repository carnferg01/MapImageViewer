using System;
using Windows.UI.Xaml.Data;

namespace MapImageViewer.Converters
{
    public class InkCanvasToNewPosition : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, string language)
        {
            return -((double)value * 5.5);
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}