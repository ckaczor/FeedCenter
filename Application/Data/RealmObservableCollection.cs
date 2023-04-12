using Realms;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FeedCenter.Data
{
    public class RealmObservableCollection<T> : ObservableCollection<T> where T : IRealmObject
    {
        private readonly Realm _realm;

        public RealmObservableCollection(Realm realm) : base(realm.All<T>())
        {
            _realm = realm;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (T item in e.OldItems)
                    _realm.Remove(item);

            if (e.NewItems != null)
                foreach (T item in e.NewItems)
                    _realm.Add(item);

            base.OnCollectionChanged(e);
        }
    }
}