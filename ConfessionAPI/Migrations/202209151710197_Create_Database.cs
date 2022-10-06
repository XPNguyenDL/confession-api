namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Create_Database : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Name = c.String(nullable: false, maxLength: 200),
                        Alias = c.String(nullable: false, maxLength: 200),
                        Description = c.String(),
                        Actived = c.Boolean(nullable: false),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Posts",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Title = c.String(nullable: false, maxLength: 500),
                        Description = c.String(),
                        CreatedTime = c.DateTime(nullable: false),
                        Like = c.Int(),
                        Dislike = c.Int(),
                        Report = c.Int(),
                        Status = c.Int(nullable: false),
                        Actived = c.Boolean(nullable: false),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Comments",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Content = c.String(nullable: false, maxLength: 1000),
                        PostTime = c.DateTime(nullable: false),
                        Like = c.Int(),
                        Dislike = c.Int(),
                        Report = c.Int(),
                        Actived = c.Boolean(nullable: false),
                        LevelComment = c.Int(),
                        AccountId = c.String(maxLength: 128),
                        PostId = c.Guid(nullable: false),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Posts", t => t.PostId, cascadeDelete: true)
                .ForeignKey("dbo.Accounts", t => t.AccountId)
                .Index(t => t.AccountId)
                .Index(t => t.PostId);
            
            CreateTable(
                "dbo.CommentLikes",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        UserID = c.String(nullable: false, maxLength: 500),
                        TimeLike = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.Id, t.UserID })
                .ForeignKey("dbo.Comments", t => t.Id, cascadeDelete: true)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.Accounts",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                        Discriminator = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.UserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Accounts", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.UserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.Accounts", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.PostHistories",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        PostId = c.Guid(nullable: false),
                        AccountId = c.String(maxLength: 128),
                        ActionTime = c.DateTime(nullable: false),
                        HistoryAction = c.Int(nullable: false),
                        OriginalPost = c.String(storeType: "ntext"),
                        ModifiedPost = c.String(storeType: "ntext"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Accounts", t => t.AccountId)
                .ForeignKey("dbo.Posts", t => t.PostId, cascadeDelete: true)
                .Index(t => t.PostId)
                .Index(t => t.AccountId);
            
            CreateTable(
                "dbo.UserInRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.Roles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.Accounts", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.UserProfiles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        FirstName = c.String(nullable: false, maxLength: 200),
                        LastName = c.String(nullable: false, maxLength: 300),
                        Birthday = c.DateTime(nullable: false),
                        Gender = c.Int(nullable: false),
                        Description = c.String(),
                        Major = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Accounts", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.Pictures",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Path = c.String(maxLength: 1000),
                        Actived = c.Boolean(nullable: false),
                        PostId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Posts", t => t.PostId, cascadeDelete: true)
                .Index(t => t.PostId);
            
            CreateTable(
                "dbo.PostLikes",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        UserID = c.String(nullable: false, maxLength: 500),
                        TimeLike = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.Id, t.UserID })
                .ForeignKey("dbo.Posts", t => t.Id, cascadeDelete: true)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.PostCategory",
                c => new
                    {
                        CategotyId = c.Guid(nullable: false),
                        PostId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.CategotyId, t.PostId })
                .ForeignKey("dbo.Categories", t => t.CategotyId, cascadeDelete: true)
                .ForeignKey("dbo.Posts", t => t.PostId, cascadeDelete: true)
                .Index(t => t.CategotyId)
                .Index(t => t.PostId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserInRoles", "UserId", "dbo.Accounts");
            DropForeignKey("dbo.UserLogins", "UserId", "dbo.Accounts");
            DropForeignKey("dbo.UserClaims", "UserId", "dbo.Accounts");
            DropForeignKey("dbo.UserInRoles", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.PostCategory", "PostId", "dbo.Posts");
            DropForeignKey("dbo.PostCategory", "CategotyId", "dbo.Categories");
            DropForeignKey("dbo.PostLikes", "Id", "dbo.Posts");
            DropForeignKey("dbo.Pictures", "PostId", "dbo.Posts");
            DropForeignKey("dbo.Comments", "AccountId", "dbo.Accounts");
            DropForeignKey("dbo.UserProfiles", "Id", "dbo.Accounts");
            DropForeignKey("dbo.PostHistories", "PostId", "dbo.Posts");
            DropForeignKey("dbo.PostHistories", "AccountId", "dbo.Accounts");
            DropForeignKey("dbo.Comments", "PostId", "dbo.Posts");
            DropForeignKey("dbo.CommentLikes", "Id", "dbo.Comments");
            DropIndex("dbo.PostCategory", new[] { "PostId" });
            DropIndex("dbo.PostCategory", new[] { "CategotyId" });
            DropIndex("dbo.Roles", "RoleNameIndex");
            DropIndex("dbo.PostLikes", new[] { "Id" });
            DropIndex("dbo.Pictures", new[] { "PostId" });
            DropIndex("dbo.UserProfiles", new[] { "Id" });
            DropIndex("dbo.UserInRoles", new[] { "RoleId" });
            DropIndex("dbo.UserInRoles", new[] { "UserId" });
            DropIndex("dbo.PostHistories", new[] { "AccountId" });
            DropIndex("dbo.PostHistories", new[] { "PostId" });
            DropIndex("dbo.UserLogins", new[] { "UserId" });
            DropIndex("dbo.UserClaims", new[] { "UserId" });
            DropIndex("dbo.Accounts", "UserNameIndex");
            DropIndex("dbo.CommentLikes", new[] { "Id" });
            DropIndex("dbo.Comments", new[] { "PostId" });
            DropIndex("dbo.Comments", new[] { "AccountId" });
            DropTable("dbo.PostCategory");
            DropTable("dbo.Roles");
            DropTable("dbo.PostLikes");
            DropTable("dbo.Pictures");
            DropTable("dbo.UserProfiles");
            DropTable("dbo.UserInRoles");
            DropTable("dbo.PostHistories");
            DropTable("dbo.UserLogins");
            DropTable("dbo.UserClaims");
            DropTable("dbo.Accounts");
            DropTable("dbo.CommentLikes");
            DropTable("dbo.Comments");
            DropTable("dbo.Posts");
            DropTable("dbo.Categories");
        }
    }
}
