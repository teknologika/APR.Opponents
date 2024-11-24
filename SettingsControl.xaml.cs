using SimHub.Plugins.Styles;
using System.Windows.Controls;
using System.Windows.Markup;

namespace APR.SimhubPlugins {
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl, IComponentConnector {
        public APRiRacing Plugin { get; }
        // public PluginSettings Settings { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(APRiRacing plugin) : this()
        {
            this.Plugin = plugin;

        }

        // public void SettingsUpdated_Click(object sender, System.Windows.RoutedEventArgs e) => Settings.SettingsUpdated = true;

    }
}