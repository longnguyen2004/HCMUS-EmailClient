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
using EmailClient.Database;
using Microsoft.EntityFrameworkCore;

namespace EmailClient.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly EmailContext _context = new("test.db");
        private CollectionViewSource emailCollectionViewSource;

        public MainWindow()
        {
            InitializeComponent();
            emailCollectionViewSource = (CollectionViewSource)FindResource(nameof(emailCollectionViewSource));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => {
                _context.Database.Migrate();
                _context.Emails.Load();
            });
            emailCollectionViewSource.Source = _context.Emails.Local.ToObservableCollection();
        }

        private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;

            if (listBoxItem?.DataContext is EmailEntry selectedEmail)
            {
                // Update the IsRead property to true
                selectedEmail.IsRead = true;

                // Save changes to the database
                _context.SaveChanges();

                // Refresh the collection view to reflect the changes in the ListBox
                emailCollectionViewSource.View.Refresh();
            }
        }
    }
}
