﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace FeedCenter
{
    public partial class FeedCenterEntities
    {
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var manager = ((IObjectContextAdapter) this).ObjectContext.ObjectStateManager;
                manager.ObjectStateManagerChanged -= HandleObjectStateManagerObjectStateManagerChanged;
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
                        var manager = ((IObjectContextAdapter) this).ObjectContext.ObjectStateManager;
                        manager.ObjectStateManagerChanged += HandleObjectStateManagerObjectStateManagerChanged;
                        _hookedStateManager = true;
                    }
                }

                return _allCategories;
            }
        }

        public Category DefaultCategory
        {
            get { return AllCategories.First(c => c.IsDefault); }
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
                        var manager = ((IObjectContextAdapter) this).ObjectContext.ObjectStateManager;
                        manager.ObjectStateManagerChanged += HandleObjectStateManagerObjectStateManagerChanged;
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
            var element = e.Element as Category;

            if (element != null)
            {
                if (_allCategories == null)
                    return;

                var category = element;

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
                        foreach (var loopCategory in Categories)
                            _allCategories.Add(loopCategory);
                        break;
                }
            }
            else if (e.Element is Feed)
            {
                if (_allFeeds == null)
                    return;

                var feed = (Feed) e.Element;

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
                        foreach (var loopfeed in Feeds)
                            _allFeeds.Add(loopfeed);
                        break;
                }
            }
        }

        #endregion
    }
}
