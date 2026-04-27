using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HRS.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // --- IDataErrorInfo Implementation ---
        
        protected readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                ValidateProperty(columnName);
                return _errors.ContainsKey(columnName) ? _errors[columnName] : null;
            }
        }

        protected virtual void ValidateProperty(string propertyName)
        {
            // Override in child classes to add validation logic
        }

        public bool IsValid => _errors.Count == 0;
        public IEnumerable<string> AllErrors => _errors.Values;

        protected void AddError(string propertyName, string error)
        {
            _errors[propertyName] = error;
            OnPropertyChanged(nameof(IsValid));
        }

        protected void RemoveError(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }
}
