using System.Data.Entity;
using ConfessionAPI.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ConfessionAPI.DAL
{
    public class ConfessionDbContext : IdentityDbContext
    {
        public ConfessionDbContext()
            : base("DefaultConnection")
        {
        }
        
        public static ConfessionDbContext Create()
        {
            return new ConfessionDbContext();
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PostHistory> PostHistories { get; set; }
        public DbSet<Picture> Pictures { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<Account> IdentityUsers { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<IdentityUserRole> UserInRoles { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<PostReport> PostReports { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region Cấu hình CSDL
            // thiết lập tên cho các bảng lưu trữ thông tin người dùng và quyền
            modelBuilder.Entity<IdentityUser>().ToTable("Accounts");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole>().ToTable("UserInRoles");
            modelBuilder.Entity<IdentityUserLogin>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserClaim>().ToTable("UserClaims");

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Posts)
                .WithMany(p => p.Categories)
                .Map(m => m.MapLeftKey("CategotyId")
                    .MapRightKey("PostId")
                    .ToTable("PostCategory"));
            #endregion
        }
    }
}