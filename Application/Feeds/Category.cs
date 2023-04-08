using Realms;
using System;
using System.Collections.Generic;

namespace FeedCenter
{
    public class Category : RealmObject
    {
        private const string DefaultCategoryName = "< default >";

        [PrimaryKey]
        [MapTo("ID")]
        public Guid Id { get; set; }
        public string Name { get; set; }

        [Ignored]
        public ICollection<Feed> Feeds { get; set; }

        public static Category Create()
        {
            return new Category { Id = Guid.NewGuid() };
        }
        public static Category CreateDefault()
        {
            return new Category { Id = Guid.NewGuid(), Name = DefaultCategoryName };
        }

        public bool IsDefault => Name == DefaultCategoryName;

        // ReSharper disable once UnusedMember.Global
        public int SortKey => IsDefault ? 0 : 1;
    }
}
