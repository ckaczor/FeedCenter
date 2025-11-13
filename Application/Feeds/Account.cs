using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Realms;

namespace FeedCenter.Feeds;

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
        AccountType.Miniflux => true,
        AccountType.Local => true,
        _ => throw new NotSupportedException()
    };

    public bool SupportsFeedDelete => Type switch
    {
        AccountType.Fever => false,
        AccountType.GoogleReader => false,
        AccountType.Miniflux => true,
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

    public async Task<int> GetProgressSteps(Account account, AccountReadInput accountReadInput)
    {
        var progressSteps = Type switch
        {
            // Delegate to the right reader based on the account type
            AccountType.Fever => await new FeverReader(account).GetProgressSteps(accountReadInput),
            AccountType.GoogleReader => await new GoogleReaderReader(account).GetProgressSteps(accountReadInput),
            AccountType.Miniflux => await new MinifluxReader(account).GetProgressSteps(accountReadInput),
            AccountType.Local => await new LocalReader(account).GetProgressSteps(accountReadInput),
            _ => throw new NotSupportedException()
        };

        return progressSteps;
    }

    public async Task<AccountReadResult> Read(AccountReadInput accountReadInput)
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

        AccountReadResult accountReadResult;
        switch (Type)
        {
            // Delegate to the right reader based on the account type
            case AccountType.Fever:
                accountReadResult = await new FeverReader(this).Read(accountReadInput);
                break;
            case AccountType.GoogleReader:
                accountReadResult = await new GoogleReaderReader(this).Read(accountReadInput);
                break;
            case AccountType.Miniflux:
                accountReadResult = await new MinifluxReader(this).Read(accountReadInput);
                break;
            case AccountType.Local:
                accountReadResult = await new LocalReader(this).Read(accountReadInput);
                break;
            default:
                throw new NotSupportedException();
        }

        return accountReadResult;
    }
}