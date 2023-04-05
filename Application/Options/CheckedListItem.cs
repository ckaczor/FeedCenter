using System.ComponentModel;

namespace FeedCenter.Options;

public class CheckedListItem<T> : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private bool _isChecked;
    private readonly T _item;

    public CheckedListItem() { }

    public CheckedListItem(T item, bool isChecked = false)
    {
        _item = item;
        _isChecked = isChecked;
    }

    public T Item
    {
        get => _item;
        init
        {
            _item = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
        }
    }
}