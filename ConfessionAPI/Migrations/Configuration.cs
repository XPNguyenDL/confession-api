using ConfessionAPI.DAL;

namespace ConfessionAPI.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<ConfessionAPI.DAL.ConfessionDbContext>
    {
        public Configuration()
        {
            // Fix lose data
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(ConfessionAPI.DAL.ConfessionDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
            AccountSeeder.Seed(context);
        }
    }
}
