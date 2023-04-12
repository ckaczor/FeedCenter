using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace FeedCenter
{
    internal class DataErrorDictionary : Dictionary<string, List<string>>
    {
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return TryGetValue(propertyName, out var value) ? value : null;
        }

        public void AddError(string propertyName, string error)
        {
            if (!ContainsKey(propertyName))
                this[propertyName] = new List<string>();

            if (this[propertyName].Contains(error))
                return;

            this[propertyName].Add(error);
            OnErrorsChanged(propertyName);
        }

        public void ClearErrors(string propertyName)
        {
            if (!ContainsKey(propertyName))
                return;

            Remove(propertyName);
            OnErrorsChanged(propertyName);
        }
    }
}