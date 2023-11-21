using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmailClient.Gui.Dialog
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>

    public partial class Login : Window
    {
        public Configuration.Login LocalLogin { get; }
        public Login()
        {
            InitializeComponent();
            LocalLogin = (Configuration.Login)((App)Application.Current).GlobalConfig.General.Clone();
            DataContext = LocalLogin;
            passwordBox.Password = LocalLogin.Password;
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            LocalLogin.Password = passwordBox.Password;
            DialogResult = true;
        }
        private void DontSave(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
