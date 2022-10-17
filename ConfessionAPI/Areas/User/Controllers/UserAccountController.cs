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
using ConfessionAPI.Areas.User.Data;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

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
                }

                if (account.UserProfile.Avatar == null)
                {
                    account.UserProfile.Avatar = "";
                }
                account.RoleTemps = temp;
                return Json(account);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateAccount()
        {
            try
            {
                var data = HttpContext.Current.Request["Account"];
                Account userUpdate = JsonConvert.DeserializeObject<Account>(data);

                var userId = User.Identity.GetUserId();
                var user = db.IdentityUsers.Find(userId);
                userUpdate.UserProfile.Id = user.UserProfile.Id;
                user.UserProfile = userUpdate.UserProfile;
                user.Email = userUpdate.Email;
                user.PhoneNumber = userUpdate.PhoneNumber;

                var ctx = HttpContext.Current;
                var root = ctx.Server.MapPath("~/Uploads/Pictures/User/" + userId);
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
                        user.UserProfile.Avatar = userId + "/" + name;
                        File.Move(localFileName, filePath);

                    }).Unwrap();

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
