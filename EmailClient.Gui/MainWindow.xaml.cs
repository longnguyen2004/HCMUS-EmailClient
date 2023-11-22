using System;
using System.Collections.Generic;
using System.Globalization;
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
using EmailClient.Gui.Component;
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
            AccountBar.Header = app.GlobalConfig.General.Email;

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
            AccountBar.Header = "";
            EmailBox.Items.Clear();
            DataContext = null;
            if (_context == null) return;
            await Task.Run(() =>
            {
                _context.SaveChanges();
                _context.Dispose();
            }).ConfigureAwait(false);
            _context = null;
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Logout().Wait();
        }
        private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            var emailEntry = (EmailEntryViewModel)listBoxItem!.DataContext;
            emailEntry.IsRead = true;

            TabItem tab = new()
            {
                Content = new EmailViewer(),
                DataContext = emailEntry.Email
            };
            tab.SetBinding(TabItem.HeaderProperty, new Binding("Subject"));
            EmailBox.Items.Add(tab);
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

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshMailbox();
        }

        private void ComposeNewMail(object sender, RoutedEventArgs e)
        {
            TabItem tab = new()
            {
                Content = new EmailWriter()
            };
            EmailBox.Items.Add(tab);
        }
    }
}
