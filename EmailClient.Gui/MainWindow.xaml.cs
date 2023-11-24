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

            foreach (TabItem currentTab in EmailBox.Items)
            {
                if (currentTab.DataContext == emailEntry.Email)
                {
                    EmailBox.SelectedItem = currentTab;
                    return;
                }
            }

            TabItem tab = new()
            {
                Content = new EmailViewer(),
                DataContext = emailEntry.Email
            };
            tab.SetBinding(TabItem.HeaderProperty, new Binding("Subject"));
            EmailBox.Items.Add(tab);
            EmailBox.SelectedItem = tab;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await _vm.FetchMessages();
        }

        private void ComposeNewMail(object sender, RoutedEventArgs e)
        {
            TabItem tab = new()
            {
                Content = new EmailComposer()
            };
            EmailBox.Items.Add(tab);
            EmailBox.SelectedItem = tab;
        }
    }
}
