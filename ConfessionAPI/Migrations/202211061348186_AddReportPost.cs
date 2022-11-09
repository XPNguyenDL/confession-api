namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReportPost : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PostReports",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        UserID = c.String(nullable: false, maxLength: 500),
                        TimeReport = c.DateTime(nullable: false),
                        Description = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => new { t.Id, t.UserID })
                .ForeignKey("dbo.Posts", t => t.Id, cascadeDelete: true)
                .Index(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PostReports", "Id", "dbo.Posts");
            DropIndex("dbo.PostReports", new[] { "Id" });
            DropTable("dbo.PostReports");
        }
    }
}
