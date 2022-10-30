using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserCommentController : UserController
    {

        /// <summary>
        /// Phương thức tìm nhóm còn của comment
        /// từ danh sách tất cả các nhóm allCmt
        /// </summary>
        /// <param name="comment">Comment cha tìm comment con</param>
        /// <param name="allCmts">Danh sách tất cả mặt hàng</param>
        private void AddSubComment(Comment comment, List<Comment> allCmts)
        {
            comment.ChildComments = allCmts
                .Where(x => x.ParentId == comment.Id)
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
            var allCmts = db.Comments.Where(x => x.PostId == postId).ToList();

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

        [HttpPost]
        public IHttpActionResult Index()
        {
            var postId = Guid.Parse(HttpContext.Current.Request["Id"]);
            var cmts = PolulateComment(postId);
            return Json(cmts);
        }

        [HttpPost]
        public IHttpActionResult Create()
        {
            try
            {
                var cmtRequest = HttpContext.Current.Request["comment"];
                var cmt = JsonConvert.DeserializeObject<Comment>(cmtRequest);
                if (db.Posts.SingleOrDefault(s => s.Id == cmt.PostId) == null)
                {
                    ModelState.AddModelError("Error", $"Post doesn't exist");
                    return BadRequest(ModelState);
                }

                var userId = User.Identity.GetUserId();
                cmt.Id = Guid.NewGuid();
                cmt.AccountId = userId;
                cmt.PostTime = DateTime.Now;
                cmt.Active = true;
                cmt.IsEdited = false;
                cmt.Content = cmt.Content.TrimEnd('\r', '\n');

                db.Comments.Add(cmt);

                var userPost = GetUserByPost(cmt.PostId);
                var userCmt = GetUserById(userId);
                NotifyCmt(cmt, userCmt, userPost);

                db.SaveChanges();
                return Json(cmt);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult Edit()
        {
            try
            {
                var cmt = HttpContext.Current.Request["Comment"];
                Comment newCmt = JsonConvert.DeserializeObject<Comment>(cmt);
                var oldCmt = db.Comments.FirstOrDefault(s => s.Id == newCmt.Id);
                if (oldCmt.AccountId != User.Identity.GetUserId())
                {
                    ModelState.AddModelError("Error", "Bạn không thể chỉnh sửa bình luận này");
                    return BadRequest(ModelState);
                }
                oldCmt.PostTime = DateTime.Now;
                oldCmt.Content = newCmt.Content;
                oldCmt.IsEdited = true;
                db.Entry(oldCmt).State = EntityState.Modified;
                db.SaveChanges();

                return Json(oldCmt);
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
                var cmtId = Guid.Parse(HttpContext.Current.Request["Id"]);
                var cmt = db.Comments.FirstOrDefault(c => c.Id == cmtId);
                if (cmt == null)
                {
                    ModelState.AddModelError("Error", "Bình luận này không tồn tại");
                    return BadRequest(ModelState);
                }

                var postId = cmt.PostId;
                var userPost = GetUserByPost(postId);
                List<Comment> newCmts;
                Post post;
                List<Guid> listIdCmt;

                if (userPost.Id == User.Identity.GetUserId())
                {
                    listIdCmt = DeleteParentComment(postId, cmtId);
                    DeleteComment(listIdCmt);
                    db.SaveChanges();
                    newCmts = PolulateComment(postId);
                    post = db.Posts.FirstOrDefault(s => s.Id == postId);

                    if (userPost.UserProfile.NickName != null)
                    {
                        post.NickName = userPost.UserProfile.NickName;
                    }
                    else
                    {
                        post.NickName = "User@" + userPost.UserProfile.Id.Split('-')[0];
                    }

                    if (userPost.UserProfile.Avatar != null || post.PrivateMode)
                    {
                        post.Avatar = userPost.UserProfile.Avatar;
                    }
                    else
                    {
                        post.Avatar = "Default/Avatar_default.png";
                    }

                    post.Comments = newCmts;
                    return Json(post);
                }
                if (cmt.AccountId != User.Identity.GetUserId())
                {
                    ModelState.AddModelError("Error", "Bạn không thể xóa bình luận này");
                    return BadRequest(ModelState);
                }
                listIdCmt = DeleteParentComment(postId, cmtId);
                DeleteComment(listIdCmt);
                db.SaveChanges();
                newCmts = PolulateComment(postId);

                post = db.Posts.FirstOrDefault(s => s.Id == postId);

                if (userPost.UserProfile.NickName != null)
                {
                    post.NickName = userPost.UserProfile.NickName;
                }
                else
                {
                    post.NickName = "User@" + userPost.UserProfile.Id.Split('-')[0];
                }

                if (userPost.UserProfile.Avatar != null || post.PrivateMode)
                {
                    post.Avatar = userPost.UserProfile.Avatar;
                }
                else
                {
                    post.Avatar = "Default/Avatar_default.png";
                }

                post.Comments = newCmts;

                return Json(post);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
            
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

        private List<Guid> DeleteParentComment(Guid postId, Guid idCmt)
        {
            var allCmts = db.Comments.Where(x => x.PostId == postId).ToList();

            var groupCmts = allCmts
                .Where(x => x.Id == idCmt)
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

        private void NotifyCmt(Comment cmt, Account userCmt, Account userPost)
        {
            if (userCmt.Id != userPost.Id)
            {
                var notifyPost = new Notification()
                {
                    Id = Guid.NewGuid(),
                    Avatar = userCmt.UserProfile.Avatar,
                    IsRead = false,
                    NotifyName = userCmt.UserProfile.NickName,
                    NotifyDate = DateTime.Now,
                    Description = $" đã bình luận bài viết của bạn!",
                    UserID = userPost.Id,
                    NotifyUserId = userCmt.Id,
                    TypeNotify = TypeNotify.Comment,
                    PostId = cmt.PostId
                };
                db.Notification.Add(notifyPost);
            }
            if (db.Comments.Any(x => x.Id == cmt.ParentId))
            {
                var userCmtParent = db.Comments.FirstOrDefault(x => x.Id == cmt.ParentId);
                if (userCmtParent.AccountId != userCmt.Id)
                {
                    var notifyCmt = new Notification()
                    {
                        Id = Guid.NewGuid(),
                        Avatar = userCmt.UserProfile.Avatar,
                        IsRead = false,
                        NotifyName = userCmt.UserProfile.NickName,
                        NotifyDate = DateTime.Now,
                        Description = $" đã trả lời bình luận của bạn!",
                        UserID = userCmtParent.AccountId,
                        NotifyUserId = userCmt.Id,
                        TypeNotify = TypeNotify.Comment,
                        PostId = cmt.PostId
                    };
                    db.Notification.Add(notifyCmt);
                }
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
