using Realms;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace FeedCenter;

public class Account : RealmObject, INotifyDataErrorInfo
{
    public const string DefaultName = "< Local >";

    private readonly DataErrorDictionary _dataErrorDictionary;

    public Account() : this(AccountType.Local)
    {
    }

    public Account(AccountType type)
    {
        Type = type;

        _dataErrorDictionary = new DataErrorDictionary();
        _dataErrorDictionary.ErrorsChanged += DataErrorDictionaryErrorsChanged;
    }

    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public AccountType Type
    {
        get => Enum.TryParse(TypeRaw, out AccountType result) ? result : AccountType.Local;
        set => TypeRaw = value.ToString();
    }

    public bool SupportsFeedEdit => Type switch
    {
        AccountType.Fever => false,
        AccountType.GoogleReader => false,
        AccountType.Local => true,
        _ => throw new NotSupportedException()
    };

    public bool SupportsFeedDelete => Type switch
    {
        AccountType.Fever => false,
        AccountType.GoogleReader => false,
        AccountType.Local => true,
        _ => throw new NotSupportedException()
    };

    private string TypeRaw { get; set; }

    public string Name
    {
        get => RawName;
        set
        {
            RawName = value;

            ValidateString(nameof(Name), RawName);
            RaisePropertyChanged();
        }
    }

    [MapTo("Name")]
    private string RawName { get; set; } = string.Empty;

    public string Url
    {
        get => RawUrl;
        set
        {
            RawUrl = value;

            ValidateString(nameof(Url), RawUrl);
            RaisePropertyChanged();
        }
    }

    [MapTo("Url")]
    public string RawUrl { get; set; }

    public string Username
    {
        get => RawUsername;
        set
        {
            RawUsername = value;

            if (!Authenticate)
            {
                _dataErrorDictionary.ClearErrors(nameof(Username));
                return;
            }

            ValidateString(nameof(Username), RawUsername);
            RaisePropertyChanged();
        }
    }

    [MapTo("Username")]
    public string RawUsername { get; set; }

    public string Password
    {
        get => RawPassword;
        set
        {
            RawPassword = value;

            if (!Authenticate)
            {
                _dataErrorDictionary.ClearErrors(nameof(Password));
                return;
            }

            ValidateString(nameof(Password), RawPassword);
            RaisePropertyChanged();
        }
    }

    [MapTo("Password")]
    public string RawPassword { get; set; }

    public bool Authenticate { get; set; }

    public bool Enabled { get; set; } = true;

    public int CheckInterval { get; set; } = 60;

    public DateTimeOffset LastChecked { get; set; }

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

    private void ValidateString(string propertyName, string value)
    {
        _dataErrorDictionary.ClearErrors(propertyName);

        if (string.IsNullOrWhiteSpace(value))
            _dataErrorDictionary.AddError(propertyName, $"{propertyName} cannot be empty");
    }

    public static Account CreateDefault()
    {
        return new Account { Name = DefaultName, Type = AccountType.Local };
    }

    public int GetProgressSteps(FeedCenterEntities entities)
    {
        var progressSteps = Type switch
        {
            // Delegate to the right reader based on the account type
            AccountType.Fever => new FeverReader().GetProgressSteps(entities),
            AccountType.GoogleReader => new GoogleReaderReader().GetProgressSteps(entities),
            AccountType.Local => new LocalReader().GetProgressSteps(entities),
            _ => throw new NotSupportedException()
        };

        return progressSteps;
    }

    public AccountReadResult Read(AccountReadInput accountReadInput)
    {
        // If not enabled then do nothing
        if (!Enabled)
            return AccountReadResult.NotEnabled;

        // Check if we're forcing a read
        if (!accountReadInput.ForceRead)
        {
            // Figure out how long since we last checked
            var timeSpan = DateTimeOffset.Now - LastChecked;

            // Check if we are due to read the feed
            if (timeSpan.TotalMinutes < CheckInterval)
                return AccountReadResult.NotDue;
        }

        var accountReadResult = Type switch
        {
            // Delegate to the right reader based on the account type
            AccountType.Fever => new FeverReader().Read(this, accountReadInput),
            AccountType.GoogleReader => new GoogleReaderReader().Read(this, accountReadInput),
            AccountType.Local => new LocalReader().Read(this, accountReadInput),
            _ => throw new NotSupportedException()
        };

        return accountReadResult;
    }
}