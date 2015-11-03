using System;

namespace FeedCenter
{
    public partial class Category
    {
        public static Category Create()
        {
            return new Category { ID = Guid.NewGuid() };
        }

        public bool IsDefault => Name == "< default >";

        // ReSharper disable once UnusedMember.Global
        public int SortKey => IsDefault ? 0 : 1;
    }
}
