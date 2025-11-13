using System.Collections.Generic;
using FeedCenter.Feeds;

namespace FeedCenter.Options;

public class AccountTypeItem
{
    public AccountType AccountType { get; set; }
    public string Name { get; set; }

    public static List<AccountTypeItem> AccountTypes =>
    [
        new()
        {
            Name = Properties.Resources.AccountTypeFever,
            AccountType = AccountType.Fever
        },
        //new()
        //{
        //    Name = Properties.Resources.AccountTypeGoogleReader,
        //    AccountType = AccountType.GoogleReader
        //}
        new()
        {
            Name = Properties.Resources.AccountTypeMiniflux,
            AccountType = AccountType.Miniflux
        }
    ];
}