using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.AspNet.Identity;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserAccountController : UserController
    {
        private ConfessionDbContext db = new ConfessionDbContext();

        [HttpGet]
        public IHttpActionResult GetInfo()
        {
            try
            {
                var account = db.IdentityUsers.Find(User.Identity.GetUserId());
                var userInRoles = db.UserInRoles.Where(s => s.UserId == account.Id).ToList();
                List<string> temp = new List<string>();
                foreach (var userRole in userInRoles)
                {
                    var role = db.Roles.Find(userRole.RoleId);
                    temp.Add(role.Name);
                    account.RoleTemps = temp;
                }
                return Json(account);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult UpdateAccount(Account userUpdate)
        {
            try
            {
                var user = db.IdentityUsers.Find(User.Identity.GetUserId());
                user.UserProfile = userUpdate.UserProfile;
                user.Email = user.Email;
                user.PhoneNumber = user.PhoneNumber;

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return Json(user);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }

        }
    }
}
