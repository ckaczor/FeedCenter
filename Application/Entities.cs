using FeedCenter.Data;
using FeedCenter.Options;
using Realms;
using System;
using System.Linq;

namespace FeedCenter;

public class FeedCenterEntities
{
    public Realm RealmInstance { get; }

    public RealmObservableCollection<Category> Categories { get; }
    public RealmObservableCollection<Feed> Feeds { get; private set; }
    public RealmObservableCollection<Setting> Settings { get; private set; }

    public FeedCenterEntities()
    {
        var realmConfiguration = new RealmConfiguration($"{Database.DatabaseFile}");

        RealmInstance = Realm.GetInstance(realmConfiguration);

        Settings = new RealmObservableCollection<Setting>(RealmInstance);
        Feeds = new RealmObservableCollection<Feed>(RealmInstance);
        Categories = new RealmObservableCollection<Category>(RealmInstance);

        if (!Categories.Any())
        {
            RealmInstance.Write(() => Categories.Add(Category.CreateDefault()));
        }
    }

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

    public Category DefaultCategory
    {
        get { return Categories.First(c => c.IsDefault); }
    }
}