using Microsoft.EntityFrameworkCore;

namespace EmailClient.Database;

public partial class EmailContext: DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailEntry>()
            .ToTable("emails");
        
        modelBuilder.Entity<EmailEntry>()
            .Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();
        
        modelBuilder.Entity<EmailEntry>()
            .Property(e => e.Filter)
            .HasColumnName("filter")
            .IsRequired(false);
        
        modelBuilder.Entity<EmailEntry>()
            .Property(e => e.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false);

        modelBuilder.Entity<EmailEntry>()
            .Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion<EmailConverter>();

        modelBuilder.Entity<EmailEntry>()
            .HasIndex(e => e.Filter);
        
        modelBuilder.Entity<EmailEntry>()
            .HasIndex(e => e.IsRead);
    }
}