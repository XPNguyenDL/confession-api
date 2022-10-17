namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNotificationAndFix : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        UserID = c.String(maxLength: 128),
                        NotifyUserId = c.String(),
                        NotifyName = c.String(),
                        Description = c.String(),
                        NotifyDate = c.DateTime(nullable: false),
                        IsRead = c.Boolean(nullable: false),
                        Avatar = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Accounts", t => t.UserID)
                .Index(t => t.UserID);
            
            AddColumn("dbo.CommentLikes", "IsLiked", c => c.Boolean(nullable: false));
            AddColumn("dbo.PostLikes", "IsLiked", c => c.Boolean(nullable: false));
            DropColumn("dbo.PostLikes", "IsLike");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PostLikes", "IsLike", c => c.Boolean(nullable: false));
            DropForeignKey("dbo.Notifications", "UserID", "dbo.Accounts");
            DropIndex("dbo.Notifications", new[] { "UserID" });
            DropColumn("dbo.PostLikes", "IsLiked");
            DropColumn("dbo.CommentLikes", "IsLiked");
            DropTable("dbo.Notifications");
        }
    }
}
