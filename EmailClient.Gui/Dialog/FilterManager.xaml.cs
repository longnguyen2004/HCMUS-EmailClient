using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EmailClient.Database;
using EmailClient.Gui;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace EmailClient.Gui.Dialog
{
    /// <summary>
    /// Interaction logic for FilterManager.xaml
    /// </summary>
    public partial class FilterManager : Window
    {
        Dictionary<string, Configuration.FilterType>  FilterTypeDic = new()  {
                { "From", Configuration.FilterType.From },
                { "Subject", Configuration.FilterType.Subject },
                { "Content", Configuration.FilterType.Content },
                { "Folder", Configuration.FilterType.Spam }
            };

        public FilterManager()
        {
            InitializeComponent();
        }
        private void Add(object sender, RoutedEventArgs e) {
            var filter = new Configuration.Filter {
                Type = FilterTypeDic[((ComboBoxItem)FilterType.SelectedItem).Name],
                Keywords = Keyword.Text.Split('|').ToList(),
                Folder = EmailTextBox.Text
            };
            (DataContext as ICollection<Configuration.Filter>)?.Add(filter);

        }
        private void Remove(object sender, RoutedEventArgs e) {
            (DataContext as ICollection<Configuration.Filter>)?.Remove((Configuration.Filter)FilterList.SelectedItem);
        }
    }
}
