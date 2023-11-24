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
using System.Windows.Shapes;

namespace EmailClient.Gui.View
{
    /// <summary>
    /// Interaction logic for EmailViewer.xaml
    /// </summary>
    public partial class EmailViewer : UserControl
    {
        public EmailViewer()
        {
            InitializeComponent();
        }
        private void SaveAttachment(object sender, EventArgs e)
        {
            var item = (IAttachment)AttachmentViewer.SelectedItem;
            if (item == null) return;
            var dialog = new Microsoft.Win32.SaveFileDialog();
            string filename = item.FileName;
            var dotIndex = filename.LastIndexOf('.');
            string ext = filename.Substring(dotIndex + 1);
            dialog.FileName = filename.Substring(0, dotIndex);
            dialog.DefaultExt = ext;
            dialog.Filter = $"All files (*.*)|*.*";
            if (dialog.ShowDialog() != true) return;
            using (FileStream fs = new FileStream(dialog.FileName, FileMode.Create))
            {
                item.ToBytes().CopyTo(fs);
            }
        }

    }
}
