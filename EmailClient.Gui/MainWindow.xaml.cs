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
using EmailClient.Gui.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace EmailClient.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EmailContext? _context;
        private EmailListViewModel _vm;
        public MainWindow()
        {
            InitializeComponent();
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
            _vm = new(_context);
            DataContext = _vm;

            await Task.Run(() => {
                _context.Database.Migrate();
                _context.Emails.Load();
            });

            await _vm.FetchMessages();
        }
        private async Task Logout()
        {
            DataContext = null;
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
            ((EmailEntryViewModel)listBoxItem!.DataContext).IsRead = true;
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
                if (_context.Emails.Find(new[]{ uid }) != null) continue;
                MemoryStream stream = new(await pop3client.GetMessage(i));
                EmailEntry emailEntry = new()
                {
                    Id = uid,
                    IsRead = false,
                    Email = new Email((await MimeParser.Parse(stream))!)
                };
                _context.Emails.Add(emailEntry);
            }
            await Task.Run(() => _context.SaveChanges());
            await pop3client.Disconnect();
        }
    }
}
