using System;
using System.Linq;
using FeedCenter.Data;
using FeedCenter.Options;
using Realms;

namespace FeedCenter;

public class FeedCenterEntities
{
    public FeedCenterEntities()
    {
        var realmConfiguration = new RealmConfiguration($"{Database.DatabaseFile}")
        {
            SchemaVersion = 1,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                var newVersionFeeds = migration.NewRealm.All<Feed>();

                foreach (var newVersionFeed in newVersionFeeds)
                {
                    if (oldSchemaVersion == 0)
                    {
                        newVersionFeed.UserAgent = null;
                    }
                }
            }
        };

        RealmInstance = Realm.GetInstance(realmConfiguration);

        Settings = new RealmObservableCollection<Setting>(RealmInstance);
        Feeds = new RealmObservableCollection<Feed>(RealmInstance);
        Categories = new RealmObservableCollection<Category>(RealmInstance);

        if (!Categories.Any())
        {
            RealmInstance.Write(() => Categories.Add(Category.CreateDefault()));
        }
    }

    public RealmObservableCollection<Category> Categories { get; }

    public Category DefaultCategory
    {
        get { return Categories.First(c => c.IsDefault); }
    }

    public RealmObservableCollection<Feed> Feeds { get; private set; }
    private Realm RealmInstance { get; }
    public RealmObservableCollection<Setting> Settings { get; private set; }

    public void Refresh()
    {
        RealmInstance.Refresh();
    }

    public void SaveChanges(Action action)
    {
        RealmInstance.Write(action);
    }

    public Transaction BeginTransaction()
    {
        return RealmInstance.BeginWrite();
    }
}