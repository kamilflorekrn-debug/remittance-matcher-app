namespace RemittanceMatcherApp.Models;

public sealed class MailboxSearchScope
{
    public string RootName { get; set; } = string.Empty;

    // Jesli puste: skanowany jest caly Inbox skrzynki RootName.
    public List<string> InboxSubfolders { get; set; } = [];

    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(RootName))
        {
            return "(pusta skrzynka)";
        }

        return InboxSubfolders.Count == 0
            ? $"{RootName} (Inbox)"
            : $"{RootName} ({InboxSubfolders.Count} podfoldery)";
    }
}
