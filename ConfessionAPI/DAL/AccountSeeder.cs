using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ConfessionAPI.DAL
{
    public class AccountSeeder
    {
        public static void Seed(ConfessionDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            const string adminRole = "Admin",
                managerRole = "Manager",
                userRole = "User";

            if (!roleManager.RoleExists(adminRole))
            {
                roleManager.Create(new IdentityRole(adminRole));
            }

            if (!roleManager.RoleExists(managerRole))
            {
                roleManager.Create(new IdentityRole(managerRole));
            }

            if (!roleManager.RoleExists(userRole))
            {
                roleManager.Create(new IdentityRole(userRole));
            }
        }
    }
}