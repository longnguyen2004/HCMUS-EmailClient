using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailClient.Database;
using Microsoft.EntityFrameworkCore;

namespace EmailClient.Gui.ViewModel;

public partial class EmailListViewModel : ObservableObject
{
    private const string _inboxStr = "Inbox";
    private static readonly Filter _inbox = new() { Name = _inboxStr };

    [ObservableProperty]
    private IEnumerable<EmailEntryViewModel> _emails = Array.Empty<EmailEntryViewModel>();
    [ObservableProperty]
    private IEnumerable<Filter> _filters = Array.Empty<Filter>();
    [ObservableProperty]
    private Filter? _currentFilter;
    partial void OnCurrentFilterChanged(Filter? value)
    {
        FilterMessages();
    }
    private readonly EmailContext _context;
    private Task? _fetchTask;
    public EmailListViewModel(EmailContext context)
    {
        _context = context;
        _context.Database.Migrate();
        _context.Emails.Load();
        _context.Filters.Include(e => e.Emails).Load();
        SyncFiltersWithDb();
        CurrentFilter = _inbox;
    }
    public void SyncFiltersWithDb()
    {
        var app = (App)Application.Current;
        foreach (var filter in app.GlobalConfig.Filters)
            if (_context.Filters.Find(filter.Folder) == null)
                _context.Filters.Local.Add(new() { Name = filter.Folder });
        _context.SaveChanges();
        Filters = new List<Filter> { _inbox }.Concat(_context.Filters.Local);
    }
    public void FilterMessages()
    {
        IEnumerable<EmailEntry> filteredEmails;
        if (CurrentFilter == null)
        {
            filteredEmails = _context.Emails;
        }
        else
        {
            var spamFolder = _context.Filters.Find("Spam");
            switch (CurrentFilter.Name)
            {
                case _inboxStr:
                    filteredEmails = _context.Emails;
                    if (spamFolder != null)
                        filteredEmails = filteredEmails.Where(e => !e.Filters.Contains(spamFolder));
                    break;

                default:
                    filteredEmails = CurrentFilter.Emails;
                    break;
            }
        }
        Emails = filteredEmails.Select(e => new EmailEntryViewModel(e));
    }
    public Task FetchMessages()
    {
        if (_fetchTask != null && !_fetchTask.IsCompleted)
            return _fetchTask;

        _fetchTask = new Task(async () =>
        {
            var app = (App)Application.Current;
            Pop3Client pop3client = new(app.GlobalConfig.General.Pop3Host, app.GlobalConfig.General.Pop3Port);
            await pop3client.Connect();
            await pop3client.Login(app.GlobalConfig.General.Email, app.GlobalConfig.General.Password);
            List<string> mailList = await pop3client.GetListing();
            List<EmailEntry> newEmails = new();
            int i = 0;
            foreach (var uid in mailList)
            {
                ++i;
                if (_context.Emails.Find(uid) != null) continue;
                MemoryStream stream = new(await pop3client.GetMessage(i));
                EmailEntry emailEntry = new()
                {
                    Id = uid,
                    IsRead = false,
                    Email = new Email((await MimeParser.Parse(stream))!)
                };
                newEmails.Add(emailEntry);
            }
            await pop3client.Disconnect();
            if (newEmails.Count == 0) return;
            EmailFilter.ApplyFilters(newEmails, app.GlobalConfig.Filters, _context.Filters);
            await Task.Run(() =>
            {
                _context.AddRange(newEmails);
                _context.SaveChanges();
            });
            FilterMessages();
        });
        _fetchTask.Start();
        return _fetchTask;
    }
}