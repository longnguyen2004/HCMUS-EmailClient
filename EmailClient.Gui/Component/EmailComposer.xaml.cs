using EmailClient.Gui.Dialog;
using EmailClient.Gui.ViewModel;
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
    public partial class EmailComposer : UserControl
    {
        public EmailComposerViewModel viewModel { get; set; } = new();
        public EmailComposer()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void AddorRemove_To(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMail()
            {
                DataContext = viewModel.To
            };
            dialog.ShowDialog();
        }
        private void AddorRemove_Cc(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMail()
            {
                DataContext = viewModel.Cc
            };
            dialog.ShowDialog();
        }
        private void AddorRemove_Bcc(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMail()
            {
                DataContext = viewModel.Bcc
            };
            dialog.ShowDialog();    
        }
    }
}
