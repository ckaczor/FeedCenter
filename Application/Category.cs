//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FeedCenter
{
    using System;
    using System.Collections.Generic;
    
    public partial class Category
    {
        public Category()
        {
            this.Feeds = new HashSet<Feed>();
        }
    
        public System.Guid ID { get; set; }
        public string Name { get; set; }
    
        public virtual ICollection<Feed> Feeds { get; set; }
    }
}
