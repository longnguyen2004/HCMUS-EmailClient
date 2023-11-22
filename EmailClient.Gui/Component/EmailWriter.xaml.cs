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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmailClient.Gui.Component
{
    /// <summary>
    /// Interaction logic for EmailWriter.xaml
    /// </summary>
    public partial class EmailWriter : UserControl
    {
        public EmailWriter()
        {
            InitializeComponent();
        }

        private void CcButton_Click(object sender, RoutedEventArgs e)
        {
            CcLabel.Visibility = Visibility.Visible;
            CcTextBox.Visibility = Visibility.Visible;
        }

        private void BccButton_Click(object sender, RoutedEventArgs e)
        {
            BccLabel.Visibility = Visibility.Visible;
            BccTextBox.Visibility = Visibility.Visible;
        }
    }
}
