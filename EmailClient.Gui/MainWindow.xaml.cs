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
using EmailClient.Gui.Control;
using EmailClient.Gui.View;
using EmailClient.Gui.ViewModel;
using EmailClient.Gui.Dialog;
using EmailClient.Gui.Converter;
using System.Threading;

namespace EmailClient.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EmailContext? _context;
        private EmailListViewModel? _vm;
        private CancellationTokenSource? _cancelAutoload;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Login()
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

            _cancelAutoload = new();
            var delay = app.GlobalConfig.General.Autoload;
            new Task(async () =>
            {
                if (delay == 0)
                    return;
                while (!_cancelAutoload.Token.IsCancellationRequested)
                {
                    if (_vm != null)
                        await _vm.FetchMessages();
                    await Task.Delay((int)delay * 1000);
                }
            }, _cancelAutoload.Token).Start();
        }
        private async Task Logout()
        {
            AccountBar.Header = "";
            EmailBox.Items.Clear();
            DataContext = null;
            if (_cancelAutoload != null)
                _cancelAutoload.Cancel();
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
            Login();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Login();
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

            CloseableTabItem tab = new()
            {
                Content = new EmailViewer(),
                DataContext = emailEntry.Email,
                MaxWidth = 150
            };
            tab.SetBinding(TabItem.HeaderProperty, new Binding("Subject"));
            EmailBox.Items.Add(tab);
            EmailBox.SelectedItem = tab;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm != null)
            {
                await _vm.FetchMessages();
            }
        }

        private void ComposeNewMail(object sender, RoutedEventArgs e)
        {
            EmailComposer composer = new();
            CloseableTabItem tab = new()
            {
                Content = composer,
                MaxWidth = 150
            };
            tab.SetBinding(TabItem.HeaderProperty, new Binding(nameof(composer.viewModel.Subject))
            {
                Source = composer.viewModel,
                Converter = new EmptyStringFallbackConverter(),
                ConverterParameter = "New Email",
                Mode = BindingMode.OneWay
            });
            EmailBox.Items.Add(tab);
            EmailBox.SelectedItem = tab;
        }
        private void OpenFilterManager(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var dialog = new FilterManager()
            {
                DataContext = app.GlobalConfig.Filters
            };
            dialog.ShowDialog();
            _ = app.SaveConfig();
            _vm?.SyncFiltersWithDb();
        }
    }
}
