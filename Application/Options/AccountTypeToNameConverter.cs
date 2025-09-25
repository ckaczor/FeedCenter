using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FeedCenter.Options;

public class AccountTypeToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AccountType accountType)
            return AccountTypeItem.AccountTypes.First(at => at.AccountType == accountType).Name;

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}