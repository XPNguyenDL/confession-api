namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsEditedCmt : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Comments", "IsEdited", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Comments", "IsEdited");
        }
    }
}
