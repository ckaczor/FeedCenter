using Realms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FeedCenter
{
    public class Category : RealmObject, INotifyDataErrorInfo
    {
        public const string DefaultName = "< default >";

        private readonly Dictionary<string, List<string>> _errorsByPropertyName = new();

        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MapTo("Name")]
        private string RawName { get; set; } = string.Empty;

        public string Name
        {
            get => RawName;
            set
            {
                RawName = value;

                ValidateName();
                RaisePropertyChanged();
            }
        }

        [Ignored]
        public ICollection<Feed> Feeds { get; set; }

        public static Category CreateDefault()
        {
            return new Category { Name = DefaultName, IsDefault = true };
        }

        public bool IsDefault { get; internal set; }

        // ReSharper disable once UnusedMember.Global
        public int SortKey => IsDefault ? 0 : 1;

        public bool HasErrors => _errorsByPropertyName.Any();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            return _errorsByPropertyName.TryGetValue(propertyName, out var value) ? value : null;
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ValidateName()
        {
            ClearErrors(nameof(Name));

            if (string.IsNullOrWhiteSpace(Name))
                AddError(nameof(Name), "Name cannot be empty");
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errorsByPropertyName.ContainsKey(propertyName))
                _errorsByPropertyName[propertyName] = new List<string>();

            if (_errorsByPropertyName[propertyName].Contains(error))
                return;

            _errorsByPropertyName[propertyName].Add(error);
            OnErrorsChanged(propertyName);
        }

        private void ClearErrors(string propertyName)
        {
            if (!_errorsByPropertyName.ContainsKey(propertyName))
                return;

            _errorsByPropertyName.Remove(propertyName);
            OnErrorsChanged(propertyName);
        }
    }
}