using System;
using System.IO;
using Honeycomb.Models;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Data;

public class AppDbContext : DbContext
{
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    public string DbPath { get; }

    public AppDbContext()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Honeycomb");
        Directory.CreateDirectory(appData);
        DbPath = Path.Combine(appData, "honeycomb.db");
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        DbPath = string.Empty;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
            options.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Code).IsRequired().HasMaxLength(10);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.SortOrder).HasDefaultValue(0);
            entity.HasData(new Category { Id = 1, Name = "預設", SortOrder = 0 });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.ExtraCost).HasDefaultValue(0m);
            entity.Property(p => p.Discount).HasDefaultValue(1m);
            entity.Property(p => p.ListingPrice).HasDefaultValue(0m);
            entity.Property(p => p.CommissionFee).HasDefaultValue(15m);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("datetime('now','localtime')");
            entity.Ignore(p => p.CostPrice);
            entity.Ignore(p => p.Profit);
            entity.Ignore(p => p.ProfitMargin);
            entity.HasOne(p => p.Currency)
                  .WithMany()
                  .HasForeignKey(p => p.CurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(p => p.CategoryId).HasDefaultValue(1);
            entity.HasOne(p => p.Category)
                  .WithMany()
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
