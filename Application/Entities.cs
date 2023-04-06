using FeedCenter.Data;
using FeedCenter.Options;
using Realms;
using System;
using System.Linq;

namespace FeedCenter
{
    public class FeedCenterEntities
    {
        public Realm Realm { get; private set; }

        public RealmObservableCollection<Category> Categories { get; private set; }
        public RealmObservableCollection<Feed> Feeds { get; private set; }
        public RealmObservableCollection<Setting> Settings { get; private set; }

        public FeedCenterEntities()
        {
            var realmConfiguration = new RealmConfiguration($"{Database.DatabaseFile}");

            Realm = Realm.GetInstance(realmConfiguration);

            Settings = new RealmObservableCollection<Setting>(Realm);
            Feeds = new RealmObservableCollection<Feed>(Realm);
            Categories = new RealmObservableCollection<Category>(Realm);

            if (!Categories.Any())
            {
                Realm.Write(() => Categories.Add(Category.CreateDefault()));
            }
        }

        public void Refresh()
        {
            Realm.Refresh();
        }

        public void SaveChanges(Action action)
        {
            Realm.Write(action);
        }

        public Category DefaultCategory
        {
            get { return Categories.First(c => c.IsDefault); }
        }
    }
}