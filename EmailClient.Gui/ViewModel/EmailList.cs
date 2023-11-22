using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailClient.Database;
using Microsoft.EntityFrameworkCore;

namespace EmailClient.Gui.ViewModel;

public partial class EmailListViewModel: ObservableObject
{
    [ObservableProperty]
    private IEnumerable<EmailEntryViewModel> _emails = Array.Empty<EmailEntryViewModel>();
    private EmailContext _context;
    public EmailListViewModel(EmailContext context)
    {
        _context = context;
    }
    public async Task FetchMessages()
    {
        Emails = await _context.Emails.Select(obj => new EmailEntryViewModel(obj)).ToArrayAsync();
    }
}