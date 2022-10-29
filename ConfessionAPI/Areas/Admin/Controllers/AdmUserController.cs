using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;

namespace ConfessionAPI.Areas.Admin.Controllers
{
    public class AdmUserController : AdmController
    {
        
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public IHttpActionResult GetUserInfo()
        {
            var listAccounts = db.IdentityUsers.ToList();
            foreach (var account in listAccounts)
            {
                var userInRoles = db.UserInRoles.Where(s => s.UserId == account.Id).ToList();
                List<string> temp = new List<string>();
                foreach (var userRole in userInRoles)
                {
                    var role = db.Roles.Find(userRole.RoleId);
                    temp.Add(role.Name);
                    account.RoleTemps = temp;
                }
            }
            
            return Json(listAccounts);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Edit()
        {
            try
            {
                var data = HttpContext.Current.Request["Account"];
                Account userUpdate = JsonConvert.DeserializeObject<Account>(data);
                var user = db.IdentityUsers.SingleOrDefault(s => s.Id == userUpdate.Id);
                if (user != null)
                {
                    userUpdate.UserProfile.Id = user.UserProfile.Id;
                    user.UserProfile = userUpdate.UserProfile;
                    user.Email = userUpdate.Email;
                    user.PhoneNumber = userUpdate.PhoneNumber;

                    var ctx = HttpContext.Current;
                    var root = ctx.Server.MapPath("~/Uploads/Pictures/User/" + user.Id);
                    if (Directory.Exists(root))
                    {
                        Directory.Delete(root, true);
                    }

                    var provider = new MultipartFormDataStreamProvider(root);

                    if (!Directory.Exists(root))
                    {
                        Directory.CreateDirectory(root);
                    }

                    await Request.Content.ReadAsMultipartAsync(provider)
                        .ContinueWith(async (a) =>
                        {
                            var file = provider.FileData.FirstOrDefault();
                            string name = file.Headers.ContentDisposition.FileName;
                            name = Guid.NewGuid() + "_" + name.Trim('"');
                            var localFileName = file.LocalFileName;
                            var filePath = Path.Combine(root, name);
                            user.UserProfile.Avatar = user.Id + "/" + name;
                            File.Move(localFileName, filePath);

                        }).Unwrap();
                    
                    if (userUpdate.RoleTemps != null)
                    {
                        var oldRole = db.UserInRoles.Where(s => s.UserId == userUpdate.Id).ToList();
                        foreach (var role in oldRole)
                        {
                            db.Entry(role).State = EntityState.Deleted;
                        }
                        foreach (var item in userUpdate.RoleTemps)
                        {
                            var idRole = db.Roles.SingleOrDefault(s => s.Name == item);
                            db.UserInRoles.Add(new IdentityUserRole()
                            {
                                // bug to đùng
                                RoleId = idRole.Id,
                                UserId = user.Id
                            });
                        }
                        db.SaveChanges();
                    }

                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return Json(user);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }

        }


        private void DeleteRoles(List<IdentityUserRole> userRoles, Account temp)
        {
            for (int i = 0; i < userRoles.Count; i++)
            {
                temp.Roles.Remove(userRoles[i]);
            }

            db.SaveChanges();
        }
    }
}
