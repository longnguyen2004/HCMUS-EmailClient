using EmailClient.Gui.Collection;
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
    /// Interaction logic for AddMail.xaml
    /// </summary>
    public partial class AddMail : Window
    {
        public AddMail()
        {
            InitializeComponent();
        }
        private void Add(object sender, RoutedEventArgs e)
        {
            try
            {
                var newEmail = new Email.EmailAddress(EmailTextBox.Text);
                ((ObservableIndexedSet<Email.EmailAddress>)DataContext).Add(newEmail);
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Email không hợp lệ", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Remove(object sender, RoutedEventArgs e)
        {
            var selected = EmailList.SelectedItems;
            foreach (Email.EmailAddress item in selected)
            {
                ((ObservableIndexedSet<Email.EmailAddress>)DataContext).Remove(item);
            }
        }

    }
}
