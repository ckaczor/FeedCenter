using Realms;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FeedCenter.Data;

public class RealmObservableCollection<T>(Realm realm) : ObservableCollection<T>(realm.All<T>()) where T : IRealmObject
{
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (T item in e.OldItems)
                realm.Remove(item);

        if (e.NewItems != null)
            foreach (T item in e.NewItems)
                realm.Add(item);

        base.OnCollectionChanged(e);
    }
}