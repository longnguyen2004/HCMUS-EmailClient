using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailClient.Database;

public class EmailEntry
{
    public string Id { get; set; }
    public string? Filter { get; set; }
    public bool IsRead { get; set; }
    public Email Email { get; set; }
}

public partial class EmailContext: DbContext
{
    public DbSet<EmailEntry> Emails { get; set; }
    public string DbPath { get; }
    public EmailContext(string dbPath)
    {
        DbPath = dbPath;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}

public class EmailContextFactory: IDesignTimeDbContextFactory<EmailContext>
{
    public EmailContext CreateDbContext(string[] args)
    {
        return new EmailContext("blah");
    }
}