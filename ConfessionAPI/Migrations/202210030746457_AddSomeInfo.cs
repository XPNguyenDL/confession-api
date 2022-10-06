namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSomeInfo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "PrivateMode", c => c.Boolean(nullable: false));
            AddColumn("dbo.UserProfiles", "Avatar", c => c.String(maxLength: 500));
            AddColumn("dbo.PostLikes", "IsLike", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PostLikes", "IsLike");
            DropColumn("dbo.UserProfiles", "Avatar");
            DropColumn("dbo.Posts", "PrivateMode");
        }
    }
}
