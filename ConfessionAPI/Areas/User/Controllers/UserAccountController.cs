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

                account.Comments.Clear();
                account.PostHistory.Clear();
                account.RoleTemps = temp;
                account.Notifications.OrderByDescending(s => s.NotifyDate);
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
                var totalFiles = HttpContext.Current.Request.Files.Count;

                var userId = User.Identity.GetUserId();
                

                var user = db.IdentityUsers.Find(userId);
                userUpdate.UserProfile.Id = user.UserProfile.Id;

                // set tạm
                user.UserProfile.Description = userUpdate.UserProfile.Description;
                user.UserProfile.NickName = userUpdate.UserProfile.NickName;

                if (totalFiles == 1)
                {
                    var ctx = HttpContext.Current;
                    var root = ctx.Server.MapPath("~/Uploads/Pictures/User/" + userId);
                    if (Directory.Exists(root))
                    {
                        Directory.Delete(root, true);
                        ProcessDirectory(ctx.Server.MapPath("~/Uploads/Pictures/User/"));
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
                }

                db.Entry(user).State = EntityState.Modified;

                var notifies = db.Notification.Where(s => s.NotifyUserId == userId).ToList();
                foreach (var notify in notifies)
                {
                    notify.NotifyName = user.UserProfile.NickName;
                    notify.Avatar = user.UserProfile.Avatar;
                    db.Entry(notify).State = EntityState.Modified;
                }

                db.SaveChanges();
                user.Comments.Clear();
                user.PostHistory.Clear();
                user.Notifications.OrderByDescending(s => s.NotifyDate);
                return Json(user);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        private void ProcessDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                ProcessDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}
