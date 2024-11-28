using MahApps.Metro.IconPacks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace APR.SimhubPlugins
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class PluginSettings : INotifyPropertyChanged 
    {
        public bool SettingsUpdated { get; set; } = false;

        public bool ShowDebug { get; set; } = false;
        public double LowFuelWarningLevel = 5.0;
        public int MAX_CARS = 64;
        public int RelativeMaxCarsAheadBehind { get; set; } = 5;
        public bool RelativeShowCarsInPits { get; set; } = false;
        public bool OverrideJavaScriptFunctions { get; set; } = true;

        #region Utility methods to refresh the UI see https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged?view=netframework-4.7.2

        protected void OnPropertyChanged(string propertyName)
          => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}