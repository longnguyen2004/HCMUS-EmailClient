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
        private EmailContext? _context;
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
                return;
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
            emailCollectionViewSource.Source = null;
            if (_context == null)
                throw new ApplicationException("_context is null here, which it shouldn't be");
            await Task.Run(() =>
            {
                _context.SaveChanges();
                _context.Dispose();
            });
        }
        private async void LogoutThenLogin(object sender, RoutedEventArgs e)
        {
            await Logout();
            await Login();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Login();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _ = Logout();
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

        private async Task RefreshMailbox()
        {
            var app = (App)Application.Current;
            Pop3Client pop3client = new (app.GlobalConfig.General.Pop3Host, app.GlobalConfig.General.Pop3Port);
            await pop3client.Connect();
            await pop3client.Login(app.GlobalConfig.General.Email, app.GlobalConfig.General.Password);
            List<string> mailList = await pop3client.GetListing();
            int i = 0;
            foreach (var uid in mailList)
            {
                ++i;
                if (_context.Emails.Any(e => e.Id == uid)) continue;
                MemoryStream stream = new();
                await stream.WriteAsync(await pop3client.GetMessage(i)!);
                stream.Seek(0, SeekOrigin.Begin);
                EmailEntry emailEntry = new()
                {
                    Id = uid,
                    IsRead = false,
                    Email = new Email((await MimeParser.Parse(stream))!)
                };
                _context.Emails.Add(emailEntry);
            }
            await _context.SaveChangesAsync();
            emailCollectionViewSource.View.Refresh();
            await pop3client.Disconnect();
        }
    }
}
