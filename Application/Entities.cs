using FeedCenter.Data;
using FeedCenter.Options;
using Realms;
using System;
using System.Linq;

namespace FeedCenter;

public class FeedCenterEntities
{
    public FeedCenterEntities()
    {
        var realmConfiguration = new RealmConfiguration($"{Database.DatabaseFile}")
        {
            SchemaVersion = 2,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                if (oldSchemaVersion == 1)
                    migration.NewRealm.Add(Account.CreateDefault());

                var localAccount = migration.NewRealm.All<Account>().First(a => a.Type == AccountType.Local);

                var newVersionCategories = migration.NewRealm.All<Category>();

                foreach (var newVersionCategory in newVersionCategories)
                {
                    switch (oldSchemaVersion)
                    {
                        case 1:
                            newVersionCategory.Account = localAccount;
                            newVersionCategory.RemoteId = null;
                            break;
                    }
                }

                var newVersionFeeds = migration.NewRealm.All<Feed>();

                foreach (var newVersionFeed in newVersionFeeds)
                {
                    switch (oldSchemaVersion)
                    {
                        case 0:
                            newVersionFeed.UserAgent = null;
                            break;
                        case 1:
                            newVersionFeed.Account = localAccount;
                            newVersionFeed.RemoteId = null;
                            break;
                    }
                }

                var newVersionFeedItems = migration.NewRealm.All<FeedItem>();

                foreach (var newVersionFeedItem in newVersionFeedItems)
                {
                    switch (oldSchemaVersion)
                    {
                        case 1:
                            newVersionFeedItem.RemoteId = null;
                            break;
                    }
                }
            }
        };

        RealmInstance = Realm.GetInstance(realmConfiguration);

        Accounts = new RealmObservableCollection<Account>(RealmInstance);
        Settings = new RealmObservableCollection<Setting>(RealmInstance);
        Feeds = new RealmObservableCollection<Feed>(RealmInstance);
        Categories = new RealmObservableCollection<Category>(RealmInstance);

        if (!Accounts.Any())
        {
            RealmInstance.Write(() => Accounts.Add(Account.CreateDefault()));
        }

        if (!Categories.Any())
        {
            var localAccount = Accounts.First(a => a.Type == AccountType.Local);
            RealmInstance.Write(() => Categories.Add(Category.CreateDefault(localAccount)));
        }
    }

    public RealmObservableCollection<Category> Categories { get; }

    public Category DefaultCategory
    {
        get { return Categories.First(c => c.IsDefault); }
    }

    public Account LocalAccount
    {
        get { return Accounts.First(a => a.Type == AccountType.Local); }
    }

    public RealmObservableCollection<Feed> Feeds { get; }
    public RealmObservableCollection<Account> Accounts { get; }
    private Realm RealmInstance { get; }
    public RealmObservableCollection<Setting> Settings { get; }

    public void SaveChanges(Action action)
    {
        RealmInstance.Write(action);
    }

    public Transaction BeginTransaction()
    {
        return RealmInstance.BeginWrite();
    }
}