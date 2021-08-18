using System;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Data
{
    public class DataContext : IdentityDbContext<AppUser, AppRole, int,
    IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<UserLike> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Connection> Connections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>().HasMany(_ => _.UserRoles).WithOne(_ => _.User).HasForeignKey(_ => _.UserId).IsRequired();
            modelBuilder.Entity<AppRole>().HasMany(_ => _.UserRoles).WithOne(_ => _.Role).HasForeignKey(_ => _.RoleId).IsRequired();

            modelBuilder.Entity<UserLike>().HasKey(_ => new { _.SourceUserId, _.LikedUserId });
            modelBuilder.Entity<UserLike>().HasOne(_ => _.SourceUser).WithMany(_ => _.LikedUsers).HasForeignKey(_ => _.SourceUserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserLike>().HasOne(_ => _.LikedUser).WithMany(_ => _.LikedByUsers).HasForeignKey(_ => _.LikedUserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>().HasOne(_ => _.Recipient).WithMany(_ => _.MessagesReceived).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Message>().HasOne(_ => _.Sender).WithMany(_ => _.MessagesSent).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Photo>().HasQueryFilter(p => p.IsApproved);
            modelBuilder.ApplyUtcDateTimeConverter();
        }
    }

    public static class UtcDateAnnotation
    {
        private const String IsUtcAnnotation = "IsUtc";
        private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
          new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableConverter =
          new ValueConverter<DateTime?, DateTime?>(v => v, v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

        public static PropertyBuilder<TProperty> IsUtc<TProperty>(this PropertyBuilder<TProperty> builder, Boolean isUtc = true) =>
          builder.HasAnnotation(IsUtcAnnotation, isUtc);

        public static Boolean IsUtc(this IMutableProperty property) =>
          ((Boolean?)property.FindAnnotation(IsUtcAnnotation)?.Value) ?? true;

        /// <summary>
        /// Make sure this is called after configuring all your entities.
        /// </summary>
        public static void ApplyUtcDateTimeConverter(this ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (!property.IsUtc())
                    {
                        continue;
                    }

                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(UtcConverter);
                    }

                    if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(UtcNullableConverter);
                    }
                }
            }
        }
    }
}