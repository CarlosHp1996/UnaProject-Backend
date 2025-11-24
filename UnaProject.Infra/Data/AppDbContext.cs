using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Entities.Security;

namespace UnaProject.Infra.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Tracking> Trackings { get; set; }
        public DbSet<Address> Addresses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração de Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Description)
                    .IsRequired();

                entity.Property(p => p.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.StockQuantity)
                    .IsRequired();

                entity.Property(p => p.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(p => p.IsActive)
                    .IsRequired();

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.Property(p => p.UpdatedAt)
                    .IsRequired();

                // Relacionamentos               
                entity.HasMany(p => p.OrderItems)
                    .WithOne(oi => oi.Product)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.Attributes)
                    .WithOne(pa => pa.Product)
                    .HasForeignKey(pa => pa.ProductId);

                entity.HasOne(p => p.Inventory)
                    .WithOne(i => i.Product)
                    .HasForeignKey<Inventory>(i => i.ProductId);
            });

            // Configuração de Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(o => o.Id);

                entity.Property(o => o.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(o => o.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(o => o.PaymentStatus)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(o => o.OrderDate)
                    .IsRequired();

                entity.Property(o => o.UpdatedAt)
                    .IsRequired();

                // Relacionamentos
                entity.HasOne(o => o.User)
                    .WithMany()
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(o => o.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(o => o.Payments)
                    .WithOne(p => p.Order)
                    .HasForeignKey(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.Address)
                    .WithMany()
                    .HasForeignKey(o => o.AddressId)
                    .OnDelete(DeleteBehavior.Restrict); // Impede a exclusão de um endereço se houver pedidos associados
            });

            // Configuração de OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.HasKey(oi => oi.Id);

                entity.Property(oi => oi.Quantity)
                    .IsRequired();

                entity.Property(oi => oi.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
            });

            // Configuração de ProductAttribute
            modelBuilder.Entity<ProductAttribute>(entity =>
            {
                entity.ToTable("ProductAttributes");
                entity.HasKey(pa => pa.Id);
            });

            // Configuração de Inventory
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("Inventory");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Quantity)
                    .IsRequired();

                entity.Property(i => i.LastUpdated)
                    .IsRequired();
            });

            // Configuração de Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.TransactionId)
                    .HasMaxLength(100);

                entity.Property(p => p.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(p => p.PaymentDate)
                    .IsRequired();
            });

            // Configuração de Tracking
            modelBuilder.Entity<Tracking>(entity =>
            {
                entity.ToTable("Trackings");
                entity.HasKey(t => t.Id);
                entity.Property(o => o.TrackingNumber)
                    .HasMaxLength(50);
                entity.Property(t => t.Status)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(t => t.Description)
                    .HasMaxLength(500);
                entity.Property(t => t.Location)
                    .HasMaxLength(100);
                entity.Property(t => t.EventDate)
                    .IsRequired();
                entity.Property(t => t.CreatedAt)
                    .IsRequired();

                // Relacionamento
                entity.HasOne(th => th.Order)
                    .WithMany()
                    .HasForeignKey(th => th.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("Addresses");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Street).IsRequired().HasMaxLength(200);
                entity.Property(a => a.CompletName).IsRequired().HasMaxLength(200);
                entity.Property(a => a.City).IsRequired().HasMaxLength(100);
                entity.Property(a => a.State).IsRequired().HasMaxLength(50);
                entity.Property(a => a.ZipCode).IsRequired().HasMaxLength(20);
                entity.Property(a => a.Neighborhood).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Number).IsRequired().HasMaxLength(20);
                entity.Property(a => a.Complement).HasMaxLength(100);
                entity.Property(a => a.MainAddress).IsRequired();

                // Relacionamento com ApplicationUser
                entity.HasOne(a => a.User)
                    .WithMany(u => u.Addresses)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
