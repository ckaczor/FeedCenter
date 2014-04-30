using System;

namespace FeedCenter
{
    public partial class Category
    {
        #region Constructor

        public Category()
        {
            ID = Guid.NewGuid();
        }

        #endregion

        public bool IsDefault
        {
            get { return Name == "< default >"; }
        }

        public int SortKey
        {
            get { return IsDefault ? 0 : 1; }
        }
    }
}
