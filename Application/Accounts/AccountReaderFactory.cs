using System;

namespace FeedCenter.Accounts;

internal static class AccountReaderFactory
{
    internal static IAccountReader CreateAccountReader(Account account) =>
        account.Type switch
        {
            AccountType.Miniflux => new MinifluxReader(account),
            AccountType.Local => new LocalReader(account),
            AccountType.Fever => new FeverReader(account),
            AccountType.GoogleReader => new GoogleReaderReader(account),
            _ => throw new NotSupportedException($"Account type '{account.Type}' is not supported."),
        };
}