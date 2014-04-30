using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FeedCenter
{
    public partial class FeedCenterEntities
    {
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ObjectStateManager.ObjectStateManagerChanged -= HandleObjectStateManagerObjectStateManagerChanged;
                _hookedStateManager = false;
            }

            base.Dispose(disposing);
        }

        #endregion

        private bool _hookedStateManager;

        #region All categories

        private ObservableCollection<Category> _allCategories;

        public ObservableCollection<Category> AllCategories
        {
            get
            {
                if (_allCategories == null)
                {
                    _allCategories = new ObservableCollection<Category>(Categories);

                    if (!_hookedStateManager)
                    {
                        ObjectStateManager.ObjectStateManagerChanged += HandleObjectStateManagerObjectStateManagerChanged;
                        _hookedStateManager = true;
                    }
                }

                return _allCategories;
            }
        }

        #endregion

        #region All feeds

        private ObservableCollection<Feed> _allFeeds;

        public ObservableCollection<Feed> AllFeeds
        {
            get
            {
                if (_allFeeds == null)
                {
                    _allFeeds = new ObservableCollection<Feed>(Feeds);

                    if (!_hookedStateManager)
                    {
                        ObjectStateManager.ObjectStateManagerChanged += HandleObjectStateManagerObjectStateManagerChanged;
                        _hookedStateManager = true;
                    }
                }

                return _allFeeds;
            }
        }

        #endregion

        #region Object state manager

        void HandleObjectStateManagerObjectStateManagerChanged(object sender, CollectionChangeEventArgs e)
        {
            if (e.Element is Category)
            {
                if (_allCategories == null)
                    return;
                
                Category category = e.Element as Category;

                switch (e.Action)
                {
                    case CollectionChangeAction.Add:
                        _allCategories.Add(category);
                        break;
                    case CollectionChangeAction.Remove:
                        _allCategories.Remove(category);
                        break;
                    case CollectionChangeAction.Refresh:
                        _allCategories.Clear();
                        foreach (Category loopCategory in Categories)
                            _allCategories.Add(loopCategory);
                        break;
                }
            }
            else if (e.Element is Feed)
            {
                if (_allFeeds == null)
                    return;

                Feed feed = e.Element as Feed;

                switch (e.Action)
                {
                    case CollectionChangeAction.Add:
                        _allFeeds.Add(feed);
                        break;
                    case CollectionChangeAction.Remove:
                        _allFeeds.Remove(feed);
                        break;
                    case CollectionChangeAction.Refresh:
                        _allFeeds.Clear();
                        foreach (Feed loopfeed in Feeds)
                            _allFeeds.Add(loopfeed);
                        break;
                }                
            }
        }

        #endregion
    }
}
