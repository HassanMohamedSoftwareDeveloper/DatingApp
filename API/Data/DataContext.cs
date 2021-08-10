using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<AppUser>().HasMany(_=>_.UserRoles).WithOne(_=>_.User).HasForeignKey(_=>_.UserId).IsRequired();
            modelBuilder.Entity<AppRole>().HasMany(_=>_.UserRoles).WithOne(_=>_.Role).HasForeignKey(_=>_.RoleId).IsRequired();

            modelBuilder.Entity<UserLike>().HasKey(_ => new { _.SourceUserId, _.LikedUserId });
            modelBuilder.Entity<UserLike>().HasOne(_ => _.SourceUser).WithMany(_ => _.LikedUsers).HasForeignKey(_ => _.SourceUserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserLike>().HasOne(_ => _.LikedUser).WithMany(_ => _.LikedByUsers).HasForeignKey(_ => _.LikedUserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>().HasOne(_ => _.Recipient).WithMany(_ => _.MessagesReceived).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Message>().HasOne(_ => _.Sender).WithMany(_ => _.MessagesSent).OnDelete(DeleteBehavior.Restrict);
            
        }
    }
}