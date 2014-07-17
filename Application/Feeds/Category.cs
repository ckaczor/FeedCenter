using System;

namespace FeedCenter
{
    public partial class Category
    {
        public static Category Create()
        {
            return new Category { ID = Guid.NewGuid() };
        }

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
