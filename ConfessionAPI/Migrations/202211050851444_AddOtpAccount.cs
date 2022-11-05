namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOtpAccount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "Otp", c => c.String());
            AddColumn("dbo.Accounts", "OtpCreateDate", c => c.DateTime());
            AddColumn("dbo.Accounts", "OtpWrongTime", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "OtpWrongTime");
            DropColumn("dbo.Accounts", "OtpCreateDate");
            DropColumn("dbo.Accounts", "Otp");
        }
    }
}
