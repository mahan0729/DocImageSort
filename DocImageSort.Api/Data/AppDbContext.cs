using DocImageSort.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocImageSort.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Borrower> Borrowers => Set<Borrower>();
    public DbSet<LoanFile> LoanFiles => Set<LoanFile>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ProcessingLog> ProcessingLogs => Set<ProcessingLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Borrower>()
            .HasMany(b => b.LoanFiles)
            .WithOne(l => l.Borrower)
            .HasForeignKey(l => l.BorrowerId);

        modelBuilder.Entity<LoanFile>()
            .HasMany(l => l.Documents)
            .WithOne(d => d.LoanFile)
            .HasForeignKey(d => d.LoanFileId)
            .IsRequired(false);

        modelBuilder.Entity<ProcessingLog>()
            .HasOne(p => p.Document)
            .WithMany()
            .HasForeignKey(p => p.DocumentId)
            .IsRequired(false);
    }
}
