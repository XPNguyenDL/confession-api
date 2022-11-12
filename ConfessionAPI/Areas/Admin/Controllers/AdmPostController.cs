using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.AspNet.Identity;

namespace ConfessionAPI.Areas.Admin.Controllers
{
    public class AdmPostController : AdmController
    {

        private void AddSubComment(Comment comment, List<Comment> allCmts)
        {
            comment.ChildComments = allCmts
                .Where(x => x.ParentId == comment.Id)
                .OrderBy(s => s.PostTime)
                .ToList();
            foreach (var subCmt in comment.ChildComments)
            {
                var userId = subCmt.AccountId;
                var account = db.IdentityUsers.Find(userId);
                if (account != null)
                {
                    if (account.UserProfile.NickName != null)
                    {
                        subCmt.NickName = account.UserProfile.NickName;
                    }
                    else
                    {
                        subCmt.NickName = "User@" + account.UserProfile.Id.Split('-')[0];
                    }
                    subCmt.Avatar = account.UserProfile.Avatar;
                }
                AddSubComment(subCmt, allCmts);
            }
        }

        private List<Comment> PolulateComment(Guid postId)
        {
            var allCmts = db.Comments.Where(x => x.PostId == postId)
                .OrderBy(s => s.PostTime)
                .ToList();
            var groupCmts = allCmts
                .Where(x => !x.ParentId.HasValue || x.ParentId == null)
                .ToList();
            foreach (var cmt in groupCmts)
            {
                var userId = cmt.AccountId;
                var account = db.IdentityUsers.Find(userId);
                if (account != null)
                {
                    if (account.UserProfile.NickName != null)
                    {
                        cmt.NickName = account.UserProfile.NickName;
                    }
                    else
                    {
                        cmt.NickName = "User@" + account.UserProfile.Id.Split('-')[0];
                    }

                    cmt.Avatar = account.UserProfile.Avatar;
                }
                AddSubComment(cmt, allCmts);
            }

            return groupCmts;
        }

        private List<Post> FilterPosts(List<Post> posts)
        {
            Account account = new Account();
            foreach (var post in posts)
            {
                var postHistorys = db.PostHistories.Where(x => x.PostId == post.Id).ToList();
                foreach (var postHistory in postHistorys)
                {
                    account = db.IdentityUsers.Find(postHistory.AccountId);
                    break;
                }

                post.Comments = PolulateComment(post.Id);

                post.TotalCmt = db.Comments.Where(s => s.PostId == post.Id).Count();

                if (account.UserProfile.NickName != null)
                {
                    post.NickName = account.UserProfile.NickName;
                }
                else
                {
                    post.NickName = "User@" + account.UserProfile.Id.Split('-')[0];
                }

                if (account.UserProfile.Avatar != null || post.PrivateMode)
                {
                    post.Avatar = account.UserProfile.Avatar;
                }
                else
                {
                    post.Avatar = "Default/Avatar_default.png";
                }
            }
            
            posts = posts.OrderByDescending(x => x.CreatedTime).ToList();

            return posts;
        }

        [HttpGet]
        public IHttpActionResult Index()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                posts = FilterPosts(posts);
                return Json(posts);
            }
            catch (Exception ex)
            {
                return Json(ex);
            }
        }

        [HttpPost]
        public IHttpActionResult IgnorePost()
        {
            try
            {
                var postId = Guid.Parse(HttpContext.Current.Request["Id"]);
                var post = db.Posts.SingleOrDefault(s => s.Id == postId);
                if (post == null)
                {
                    ModelState.AddModelError("Error", "Bài viết không tồn tại.");
                    return BadRequest();
                }

                var totalReport = post.Report;
                var postActive = post.Active;

                if (post.Report > 0)
                {
                    post.Status = PostStatus.Valid;
                    post.Active = true;
                    post.Report = 0;

                    // Delete Post Report
                    var postReport = db.PostReports.Where(s => s.Id == postId);
                    foreach (var report in postReport)
                    {
                        db.PostReports.Remove(report);
                    }

                    var userPost = GetUserByPost(postId);
                    var userAdmin = GetUserById(User.Identity.GetUserId());

                    if (totalReport > 50 || postActive == false)
                    {
                        var notifyPost = new Notification()
                        {
                            Id = Guid.NewGuid(),
                            Avatar = userAdmin.UserProfile.Avatar,
                            IsRead = false,
                            NotifyName = userAdmin.UserProfile.NickName,
                            NotifyDate = DateTime.Now,
                            Description = $" đã được phê duyệt là không vi phạm!",
                            UserID = userPost.Id,
                            NotifyUserId = userAdmin.Id,
                            TypeNotify = TypeNotify.Report,
                            PostId = postId
                        };
                        db.Notification.Add(notifyPost);
                    }

                    db.Entry(post).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return Json(post);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        private Account GetUserByPost(Guid postId)
        {
            var user = new Account();

            var postHistorys = db.PostHistories.Where(x => x.PostId == postId).ToList();
            foreach (var postHistory in postHistorys)
            {
                user = db.IdentityUsers.Find(postHistory.AccountId);
                break;
            }

            if (user.UserProfile.NickName == null)
            {
                user.UserProfile.NickName = "User@" + user.UserProfile.Id.Split('-')[0];
            }

            return user;
        }

        [HttpGet]
        public IHttpActionResult PostViolate()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                posts = posts.Where(s => s.Report > 0).ToList();
                posts = FilterPosts(posts);
                posts = posts.OrderByDescending(s => s.Report).ToList();
                return Json(posts);
            }
            catch (Exception ex)
            {
                return Json(ex);
            }
        }

        [HttpGet]
        public IHttpActionResult PostHidden()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                posts = posts.Where(s => !s.Active).ToList();
                posts = FilterPosts(posts);
                return Json(posts);
            }
            catch (Exception ex)
            {
                return Json(ex);
            }
        }

        [HttpPost]
        public IHttpActionResult Delete()
        {
            try
            {
                Guid postId = Guid.Parse(HttpContext.Current.Request["Id"]);
                if (!db.Posts.Any(s => s.Id == postId))
                {
                    ModelState.AddModelError("Error", "Bài viết không tồn tại.");
                    return BadRequest(ModelState);
                }

                DeletePost(postId);
                var newPost = db.Posts.ToList();
                return Json(newPost);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult FindPostViolate()
        {
            try
            {
                string keyword = HttpContext.Current.Request["keyword"];
                keyword = keyword.Replace("\n", "").Replace("\r", "");
                keyword = RemoveSignVietnameseString(keyword).ToLower();
                var listKey = keyword.Split(' ');

                var posts = db.Posts.Where(s => s.Report > 0).ToList();
                foreach (var key in listKey)
                {
                    if (posts.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            posts = posts.Where(x =>
                                RemoveSignVietnameseString(x.Title.ToLower()).Contains(key) ||
                                RemoveSignVietnameseString(x.Content.ToLower()).Contains(key) || 
                                x.PostReports.Any(r => RemoveSignVietnameseString(r.Description).ToLower().Contains(key))||
                                x.Categories.Any(c => RemoveSignVietnameseString(c.Name).ToLower().Contains(key))).ToList();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                posts = FilterPosts(posts);
                posts = posts.OrderByDescending(s => s.Report).ToList();
                return Json(posts);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet]
        public IHttpActionResult DeleteViolate()
        {
            try
            {
                using (var context = new ConfessionDbContext())
                {
                    var listPostViolates = context.Posts.Where(s => !s.Active).ToList();
                    foreach (var post in listPostViolates)
                    {
                        DeletePost(post.Id);
                    }
                }

                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                posts = posts.Where(s => s.Report > 0).ToList();
                posts = FilterPosts(posts);
                return Json(posts);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
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
        private Account GetUserById(string id)
        {
            var user = db.IdentityUsers.Find(id);

            if (user.UserProfile.NickName == null)
            {
                user.UserProfile.NickName = "User@" + user.UserProfile.Id.Split('-')[0];
            }

            return user;
        }

    }
}
