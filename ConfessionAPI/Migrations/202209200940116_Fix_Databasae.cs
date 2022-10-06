namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Fix_Databasae : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Categories", "Active", c => c.Boolean(nullable: false));
            AddColumn("dbo.Posts", "Content", c => c.String());
            AddColumn("dbo.Posts", "Active", c => c.Boolean(nullable: false));
            AddColumn("dbo.Comments", "Active", c => c.Boolean(nullable: false));
            AddColumn("dbo.UserProfiles", "NickName", c => c.String());
            AddColumn("dbo.Pictures", "Active", c => c.Boolean(nullable: false));
            DropColumn("dbo.Categories", "Actived");
            DropColumn("dbo.Posts", "Description");
            DropColumn("dbo.Posts", "Actived");
            DropColumn("dbo.Comments", "Actived");
            DropColumn("dbo.Pictures", "Actived");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Pictures", "Actived", c => c.Boolean(nullable: false));
            AddColumn("dbo.Comments", "Actived", c => c.Boolean(nullable: false));
            AddColumn("dbo.Posts", "Actived", c => c.Boolean(nullable: false));
            AddColumn("dbo.Posts", "Description", c => c.String());
            AddColumn("dbo.Categories", "Actived", c => c.Boolean(nullable: false));
            DropColumn("dbo.Pictures", "Active");
            DropColumn("dbo.UserProfiles", "NickName");
            DropColumn("dbo.Comments", "Active");
            DropColumn("dbo.Posts", "Active");
            DropColumn("dbo.Posts", "Content");
            DropColumn("dbo.Categories", "Active");
        }
    }
}
