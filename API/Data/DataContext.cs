using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<UserLike> Likes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserLike>().HasKey(_ => new { _.SourceUserId, _.LikedUserId });
            modelBuilder.Entity<UserLike>().HasOne(_ => _.SourceUser).WithMany(_ => _.LikedUsers).HasForeignKey(_ => _.SourceUserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserLike>().HasOne(_ => _.LikedUser).WithMany(_ => _.LikedByUsers).HasForeignKey(_ => _.LikedUserId).OnDelete(DeleteBehavior.Cascade);
            base.OnModelCreating(modelBuilder);
        }
    }
}