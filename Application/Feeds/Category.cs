using JetBrains.Annotations;
using Realms;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace FeedCenter;

public class Category : RealmObject, INotifyDataErrorInfo
{
    public const string DefaultName = "< default >";

    private readonly DataErrorDictionary _dataErrorDictionary;

    public Category()
    {
        _dataErrorDictionary = new DataErrorDictionary();
        _dataErrorDictionary.ErrorsChanged += DataErrorDictionaryErrorsChanged;
    }

    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RemoteId { get; set; }

    public Account Account { get; set; }

    public bool IsDefault { get; internal set; }

    public string Name
    {
        get => RawName;
        set
        {
            RawName = value;

            ValidateName();
            RaisePropertyChanged();
        }
    }

    [MapTo("Name")]
    private string RawName { get; set; } = string.Empty;

    [UsedImplicitly]
    public int SortKey => IsDefault ? 0 : 1;

    public bool HasErrors => _dataErrorDictionary.Any();

    public IEnumerable GetErrors(string propertyName)
    {
        return _dataErrorDictionary.GetErrors(propertyName);
    }

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    private void DataErrorDictionaryErrorsChanged(object sender, DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(e.PropertyName));
    }

    public static Category CreateDefault(Account account)
    {
        return new Category { Name = DefaultName, IsDefault = true, Account = account };
    }

    private void ValidateName()
    {
        _dataErrorDictionary.ClearErrors(nameof(Name));

        if (string.IsNullOrWhiteSpace(Name))
            _dataErrorDictionary.AddError(nameof(Name), "Name cannot be empty");
    }
}