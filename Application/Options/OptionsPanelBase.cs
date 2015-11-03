using System;
using System.Windows.Controls;

namespace FeedCenter.Options
{
    public class OptionsPanelBase : UserControl
    {
        protected FeedCenterEntities Database { get; private set; }

        public virtual void LoadPanel(FeedCenterEntities database)
        {
            Database = database;
        }

        public virtual bool ValidatePanel()
        {
            throw new NotImplementedException();
        }

        public virtual void SavePanel()
        {
            throw new NotImplementedException();
        }

        public virtual string CategoryName => null;
    }
}
