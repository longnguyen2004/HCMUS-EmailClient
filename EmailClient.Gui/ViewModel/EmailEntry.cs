using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailClient.Database;

namespace EmailClient.Gui.ViewModel;

public class EmailEntryViewModel: ObservableObject
{
    readonly EmailEntry _entry;
    public string Id => _entry.Id;
    public ICollection<Filter> Filters => _entry.Filters;
    public Email Email => _entry.Email;
    public bool IsRead {
        get => _entry.IsRead;
        set {
            OnPropertyChanging(nameof(IsRead));
            _entry.IsRead = value;
            OnPropertyChanged(nameof(IsRead));
        }
    }
    public EmailEntryViewModel(EmailEntry entry)
    {
        _entry = entry;
    }
}
