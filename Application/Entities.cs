using Dapper;
using FeedCenter.Data;
using FeedCenter.Options;
using Realms;
using System;
using System.Data.SqlServerCe;
using System.IO;
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
            Load();
        }

        public void Refresh()
        {
            Realm.Refresh();
        }

        public void Load()
        {
            var realmConfiguration = new RealmConfiguration($"{Database.DatabasePath}/FeedCenter.realm");

            Realm = Realm.GetInstance(realmConfiguration);

            if (File.Exists(Database.DatabaseFile))
            {
                using var connection = new SqlCeConnection($"Data Source={Database.DatabaseFile}");

                connection.Open();

                var settings = connection.Query<Setting>("SELECT * FROM Setting").OrderBy(s => s.Version).ToList();
                var categories = connection.Query<Category>("SELECT * FROM Category").ToList();
                var feeds = connection.Query<Feed>("SELECT * FROM Feed").ToList();
                var feedItems = connection.Query<FeedItem>("SELECT * FROM FeedItem").ToList();

                Realm.Write(() =>
                {
                    foreach (var category in categories)
                    {
                        category.Feeds = feeds.Where(f => f.CategoryId == category.Id).ToList();
                    }

                    foreach (var feed in feeds)
                    {
                        feed.Category = categories.FirstOrDefault(c => c.Id == feed.CategoryId);
                    }

                    foreach (var feedItem in feedItems)
                    {
                        var feed = feeds.First(f => f.Id == feedItem.FeedId);

                        feed.Items.Add(feedItem);
                    }

                    Realm.Add(feeds);
                    Realm.Add(categories);
                    Realm.Add(settings, true);
                });

                connection.Close();

                File.Move(Database.DatabaseFile, Database.DatabaseFile + "_bak");
            }

            Settings = new RealmObservableCollection<Setting>(Realm);
            Feeds = new RealmObservableCollection<Feed>(Realm);
            Categories = new RealmObservableCollection<Category>(Realm);

            if (!Categories.Any())
            {
                Realm.Write(() => Categories.Add(Category.CreateDefault()));
            }
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