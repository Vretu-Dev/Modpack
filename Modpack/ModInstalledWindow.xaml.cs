using System.Windows;

namespace WotModpackLoader
{
    public partial class ModInstalledWindow : Window
    {
        public ModInstalledWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}