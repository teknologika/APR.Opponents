using SimHub.Plugins.Styles;
using System.Windows.Controls;

namespace APR.SimhubPlugins {
    /// <summary>
    /// Interaction logic for OpponentsSettingsControl.xaml
    /// </summary>
    public partial class OpponentsSettingsControl : UserControl
    {
        public APROpponentsPlugin Plugin { get; }

        public OpponentsSettingsControl()
        {
            InitializeComponent();
        }

        public OpponentsSettingsControl(APROpponentsPlugin plugin) : this()
        {
            this.Plugin = plugin;
        }

        private async void StyledMessageBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var res = await SHMessageBox.Show("Message box", "Hello", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);

            await SHMessageBox.Show(res.ToString());
        }

        private void DemoWindow_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var window = new  DemoWindow();

            window.Show();
        }

        private async void DemodialogWindow_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialogWindow = new DemoDialogWindow();

            var res = await dialogWindow.ShowDialogWindowAsync(this);

            await SHMessageBox.Show(res.ToString());
        }
    }
}