using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EmailClient.Gui.Collection;

namespace EmailClient.Gui.ViewModel;

public partial class EmailComposerViewModel : ObservableObject
{
    public ObservableIndexedSet<Email.EmailAddress> To { get; } = new();
    public ObservableIndexedSet<Email.EmailAddress> Cc { get; } = new();
    public ObservableIndexedSet<Email.EmailAddress> Bcc { get; } = new();
    [ObservableProperty]
    private string subject = string.Empty;
    [ObservableProperty]
    private string body = string.Empty;
    public ObservableCollection<IAttachment> Attachments { get; } = new();
}
