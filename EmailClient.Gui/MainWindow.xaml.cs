using System;
using System.Collections.Generic;
using System.IO;
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
using EmailClient.Database;
using EmailClient.Gui.Dialog;
using Microsoft.EntityFrameworkCore;

namespace EmailClient.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EmailContext _context;
        private CollectionViewSource emailCollectionViewSource;
        public MainWindow()
        {
            InitializeComponent();
            emailCollectionViewSource = (CollectionViewSource)FindResource(nameof(emailCollectionViewSource));
        }
        private async Task Login()
        {
            var login = new Login();
            var ok = login.ShowDialog();
            if (ok != true)
            {
                Close();
            }
            var app = (App)Application.Current;
            app.GlobalConfig.General = login.LocalLogin;
            _ = app.SaveConfig();

            var messagePath = Path.Join(app.RootDir, "messages");
            Directory.CreateDirectory(messagePath);
            _context = new(Path.Join(messagePath, $"{app.GlobalConfig.General.Email}.db"));

            await Task.Run(() => {
                _context.Database.Migrate();
                _context.Emails.Load();
            });
            emailCollectionViewSource.Source = _context.Emails.Local.ToObservableCollection();
        }
        private async Task Logout()
        {
            await Task.Run(() => {
                _context.SaveChanges();
                _context.Dispose();
            });
            emailCollectionViewSource.Source = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;

            if (listBoxItem?.DataContext is EmailEntry selectedEmail)
            {
                selectedEmail.IsRead = true;
                _context.SaveChanges();
                emailCollectionViewSource.View.Refresh();
            }
        }
    }
}
