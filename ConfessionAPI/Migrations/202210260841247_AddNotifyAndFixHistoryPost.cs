namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNotifyAndFixHistoryPost : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Notifications", "PostId", c => c.Guid(nullable: false));
            AddColumn("dbo.Notifications", "TypeNotify", c => c.Int(nullable: false));
            DropColumn("dbo.PostHistories", "OriginalPost");
            DropColumn("dbo.PostHistories", "ModifiedPost");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PostHistories", "ModifiedPost", c => c.String(storeType: "ntext"));
            AddColumn("dbo.PostHistories", "OriginalPost", c => c.String(storeType: "ntext"));
            DropColumn("dbo.Notifications", "TypeNotify");
            DropColumn("dbo.Notifications", "PostId");
        }
    }
}
