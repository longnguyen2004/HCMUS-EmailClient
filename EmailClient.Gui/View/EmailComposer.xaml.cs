﻿using EmailClient.Gui.Dialog;
using EmailClient.Gui.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
            ToTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }
        private void AddorRemove_Cc(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMail()
            {
                DataContext = viewModel.Cc
            };
            dialog.ShowDialog();
            CcTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }
        private void AddorRemove_Bcc(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMail()
            {
                DataContext = viewModel.Bcc
            };
            dialog.ShowDialog();
            BccTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }
        private void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() != true) return;
            System.IO.FileInfo file = new(dialog.FileName);
            if (file.Length > 3 * 1024 * 1024)
                MessageBox.Show("Kích cỡ file vượt quá 3MB", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                viewModel.Attachments.Add(new AttachmentLocal(file));
        }
        private void RemoveAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = AttachmentsListBox.SelectedItems.Cast<IAttachment>().ToList();
            foreach (var selectedItem in selectedItems)
                viewModel.Attachments.Remove(selectedItem);
        }
        private async void SendEmail(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            SmtpClient smtpClient = new(app.GlobalConfig.General.SmtpHost, app.GlobalConfig.General.SmtpPort);
            await smtpClient.Connect();
            var body = new BodyBuilder()
            {
                TextBody = viewModel.Body,
                Attachments = viewModel.Attachments
            }.GetMessageBody();
            body.Headers.Add("User-Agent", new("Inboxinator 3000"));
            Email email = new()
            {
                Date = DateTime.UtcNow,
                From = new Email.EmailAddress(app.GlobalConfig.General.Email),
                To = viewModel.To,
                Cc = viewModel.Cc,
                Bcc = viewModel.Bcc,
                Subject = viewModel.Subject,
                Body = body
            };
            await smtpClient.SendEmail(email);

            await smtpClient.Disconnect();
            MessageBox.Show("Đã gửi thành công", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
