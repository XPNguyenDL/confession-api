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
    [Authorize(Roles = "Admin")]
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
            listAccounts = FilterUsers(listAccounts);

            return Json(listAccounts);
        }

        [HttpGet]
        public IHttpActionResult GetRole()
        {
            try
            {
                var roles = db.Roles.ToList();

                return Json(roles);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult SetRolesUser()
        {
            try
            {
                var data = HttpContext.Current.Request["Account"];
                Account userUpdate = JsonConvert.DeserializeObject<Account>(data);
                var user = db.IdentityUsers.SingleOrDefault(s => s.Id == userUpdate.Id);
                if (user == null)
                {
                    ModelState.AddModelError("Error", "Tài khoản không tồn tại");
                    return BadRequest(ModelState);
                }

                if (userUpdate.RoleTemps != null)
                {
                    var oldRole = db.UserInRoles.Where(s => s.UserId == userUpdate.Id).ToList();
                    foreach (var role in oldRole)
                    {
                        db.Entry(role).State = EntityState.Deleted;
                    }
                    foreach (var item in userUpdate.RoleTemps)
                    {
                        var idRole = db.Roles.SingleOrDefault(s => s.Id == item);
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

                return Json(user);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }

        }

        [HttpPost]
        public IHttpActionResult Delete()
        {
            try
            {

                var userId = HttpContext.Current.Request["Id"];

                var user = db.IdentityUsers.SingleOrDefault(s => s.Id == userId);
                if (user == null)
                {
                    ModelState.AddModelError("Error", "Tài khoản không tồn tại.");
                    return BadRequest(ModelState);
                }

                DeletePostUser(user.Id);

                var listIdCmt = DeleteParentComment(user.Id);
                DeleteComment(listIdCmt);


                db.UserProfiles.Remove(user.UserProfile);

                var listNotifies = db.Notification.Where(s => s.UserID == user.Id).ToList();
                foreach (var notify in listNotifies)
                {
                    db.Notification.Remove(notify);
                }

                db.IdentityUsers.Remove(user);

                db.SaveChanges();

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
                    account.Comments.Clear();
                    account.PostHistory.Clear();
                    account.Notifications.Clear();

                }

                return Json(listAccounts);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult FindUser()
        {
            try
            {
                string keyword = HttpContext.Current.Request["keyword"];
                keyword = keyword.Replace("\n", "").Replace("\r", "");
                keyword = RemoveSignVietnameseString(keyword).ToLower();
                var listKey = keyword.Split(' ');

                var listUsers = db.IdentityUsers.ToList();
                listUsers = FilterUsers(listUsers);

                foreach (var key in listKey)
                {
                    if (listUsers.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            listUsers = listUsers.Where(x => x.UserName.ToLower().Contains(key) ||
                                x.Email.ToLower().Contains(key) ||
                                RemoveSignVietnameseString(new UserProfile(x.UserProfile).NickName).ToLower().Contains(key)
                                || x.RoleTemps.Any(s => s.ToLower().Contains(key))).ToList();
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                return Json(listUsers);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        private List<Account> FilterUsers(List<Account> listUsers)
        {
            foreach (var account in listUsers)
            {
                var userInRoles = db.UserInRoles.Where(s => s.UserId == account.Id).ToList();
                List<string> temp = new List<string>();
                foreach (var userRole in userInRoles)
                {
                    var role = db.Roles.Find(userRole.RoleId);
                    temp.Add(role.Name);
                    account.RoleTemps = temp;
                }
                account.Comments.Clear();
                account.PostHistory.Clear();
                account.Notifications.Clear();

            }

            listUsers = listUsers.OrderByDescending(s => s.UserProfile.Birthday).ToList();

            return listUsers;
        }

       

        private void DeletePostUser(string userId)
        {


            var listCommentLikes = db.CommentLikes.Where(s => s.UserID == userId).ToList();
            foreach (var commentLike in listCommentLikes)
            {
                db.CommentLikes.Remove(commentLike);
            }


            var listPostLikes = db.PostLikes.Where(s => s.UserID == userId);
            foreach (var postLike in listPostLikes)
            {
                db.PostLikes.Remove(postLike);
            }

            var listPosts = db.Posts.Where(s => s.PostHistories.Any(x => x.AccountId == userId)).ToList();
            foreach (var post in listPosts)
            {
                DeletePost(post.Id);
            }

            db.SaveChanges();

        }

        private void ChildComment(Comment comment, List<Comment> allCmts, List<Guid> listId)
        {
            comment.ChildComments = allCmts
                .Where(x => x.ParentId == comment.Id)
                .ToList();
            var list = new List<Guid>();
            foreach (var subCmt in comment.ChildComments)
            {
                listId.Add(subCmt.Id);
                ChildComment(subCmt, allCmts, listId);
            }
        }

        private List<Guid> DeleteParentComment(string idUser)
        {
            var allCmts = db.Comments.ToList();

            var groupCmts = allCmts
                .Where(x => x.AccountId == idUser)
                .ToList();
            var listIdCmt = new List<Guid>();
            foreach (var cmt in groupCmts)
            {
                listIdCmt.Add(cmt.Id);
                ChildComment(cmt, allCmts, listIdCmt);

            }

            var resultList = new List<Guid>();
            for (int i = listIdCmt.Count - 1; i >= 0; i--)
            {
                resultList.Add(listIdCmt[i]);
            }

            return resultList;
        }

        private void DeleteComment(List<Guid> listIdCmt)
        {

            foreach (var idCmt in listIdCmt)
            {
                var cmt = db.Comments.Find(idCmt);
                var listCmtLikes = db.CommentLikes.Where(s => s.Id == idCmt).ToList();
                foreach (var cmtLike in listCmtLikes)
                {
                    db.CommentLikes.Remove(cmtLike);
                }

                db.Comments.Remove(cmt);
            }
        }

        private void DeletePost(Guid postId)
        {
            var listPostHistorys = db.PostHistories.Where(x => x.PostId == postId).ToList();
            var oldPost = db.Posts.Find(postId);

            // Delete PostLike
            var postLikes = db.PostLikes.Where(s => s.Id == oldPost.Id).ToList();
            if (postLikes.Count != 0)
            {
                foreach (var postLike in postLikes)
                {
                    db.PostLikes.Remove(postLike);
                }
            }

            // Delete PostReport
            var postReports = db.PostReports.Where(s => s.Id == oldPost.Id).ToList();
            if (postReports.Count != 0)
            {
                foreach (var postReport in postReports)
                {
                    db.PostReports.Remove(postReport);
                }
            }

            // Delete comment
            var comments = db.Comments.Where(s => s.Id == oldPost.Id).ToList();
            if (comments.Count != 0)
            {
                foreach (var comment in comments)
                {
                    // Delete CommentLike
                    var commentLikes = db.CommentLikes.Where(s => s.Id == comment.Id);
                    foreach (var commentLike in commentLikes)
                    {
                        db.CommentLikes.Remove(commentLike);
                    }
                    db.Comments.Remove(comment);
                }
            }

            // Delete Picture
            var ctx = HttpContext.Current;
            var root = ctx.Server.MapPath("~/Uploads/Pictures/Post/" + oldPost.Id);
            if (Directory.Exists(root))
            {
                var pictures = db.Pictures.Where(x => x.PostId == oldPost.Id);
                foreach (var item in pictures)
                {
                    db.Pictures.Remove(item);
                }
                Directory.Delete(root, true);
            }

            // Delete PostHistory
            if (listPostHistorys.Count != 0)
            {
                foreach (var itemHistory in listPostHistorys)
                {
                    db.PostHistories.Remove(itemHistory);
                }
            }

            db.Posts.Remove(oldPost);
            db.SaveChanges();
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
