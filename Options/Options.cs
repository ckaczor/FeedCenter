using System;
using System.Globalization;
using System.Windows.Data;

namespace FeedCenter.Options
{
    public enum MultipleLineDisplay
    {
        Normal,
        SingleLine,
        FirstLine
    }

    public enum MultipleOpenAction
    {
        IndividualPages,
        SinglePage
    }

    [ValueConversion(typeof(int), typeof(MultipleOpenAction))]
    public class MultipleOpenActionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (MultipleOpenAction) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int) value;
        }
    }
}
