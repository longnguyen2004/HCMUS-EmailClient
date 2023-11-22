using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailClient.Database;

#nullable disable

public class EmailEntry
{
    public string Id { get; set; }
    public bool IsRead { get; set; }
    public Email Email { get; set; }
    public List<Filter> Filters { get; } = new();
}

public class Filter {
    public string Name { get; set; }
    public List<EmailEntry> Emails { get; } = new();
}

public partial class EmailContext: DbContext
{
    public DbSet<EmailEntry> Emails { get; set; }
    public DbSet<Filter> Filters { get; set; }
    public string DbPath { get; }
    public EmailContext(string dbPath)
    {
        DbPath = dbPath;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }
}

public class EmailContextFactory: IDesignTimeDbContextFactory<EmailContext>
{
    public EmailContext CreateDbContext(string[] args)
    {
        return new EmailContext("blah");
    }
}