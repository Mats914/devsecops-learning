using Microsoft.EntityFrameworkCore;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>         Users         { get; set; } = null!;
    public DbSet<Post>         Posts         { get; set; } = null!;
    public DbSet<Comment>      Comments      { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<AuditLog>     AuditLogs     { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();

        mb.Entity<User>()
            .HasIndex(u => u.Email);

        mb.Entity<Post>()
            .HasOne(p => p.Author).WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Comment>()
            .HasOne(c => c.Post).WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Comment>()
            .HasOne(c => c.Author).WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<RefreshToken>()
            .HasOne(r => r.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<RefreshToken>()
            .HasIndex(r => r.Token).IsUnique();

        mb.Entity<AuditLog>()
            .HasOne(a => a.User).WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
