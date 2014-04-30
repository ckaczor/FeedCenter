using System;

namespace FeedCenter
{
    public partial class FeedAction
    {
        #region Constructor

        public FeedAction()
        {
            ID = Guid.NewGuid();
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return string.Format(Properties.Resources.FeedActionDescription, Field, Search, Replace);
        }

        #endregion
    }
}
