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
        private ConfessionDbContext db = new ConfessionDbContext();

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
                cmt.Id = Guid.NewGuid();
                cmt.AccountId = User.Identity.GetUserId();
                cmt.PostTime = DateTime.Now;
                cmt.Active = true;
                db.Comments.Add(cmt);
                db.SaveChanges();
                return Json(cmt);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> Like()
        //{
        //    try
        //    {
        //        var post = new Post();
        //        var idPost = Guid.Parse(HttpContext.Current.Request["Id"]);
        //        post = db.Posts.Find(idPost);
        //        if (post == null)
        //        {
        //            ModelState.AddModelError("Post", "Post doesn't exist");
        //            return BadRequest(ModelState);
        //        }
        //        var userId = User.Identity.GetUserId();
        //        var postLikeCheck = db.PostLikes.SingleOrDefault(x => x.UserID == userId && x.Id == idPost);
        //        var postLike = new PostLike();
        //        int totalLike, totalDislike;

        //        if (postLikeCheck == null)
        //        {
        //            postLike.Id = idPost;
        //            postLike.UserID = User.Identity.GetUserId();
        //            postLike.TimeLike = DateTime.Now;
        //            postLike.IsLiked = true;

        //            db.PostLikes.Add(postLike);
        //            db.SaveChanges();
        //        }
        //        else
        //        {
        //            if (postLikeCheck.IsLiked == false)
        //            {
        //                postLikeCheck.TimeLike = DateTime.Now;
        //                postLikeCheck.IsLiked = true;
        //                db.Entry(postLikeCheck).State = EntityState.Modified;
        //            }
        //            else
        //            {
        //                db.PostLikes.Remove(postLikeCheck);
        //            }
        //            db.SaveChanges();
        //        }

        //        totalLike = db.PostLikes.Where(x => x.IsLiked == true
        //                                            && x.UserID == userId
        //                                            && x.Id == idPost).Count();
        //        totalDislike = db.PostLikes.Where(x => x.IsLiked == false
        //                                               && x.UserID == userId
        //                                               && x.Id == idPost).Count();
        //        post.Like = totalLike;
        //        post.Dislike = totalDislike;
        //        db.Entry(post).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return Json(post);

        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("Error", e.Message);
        //        return BadRequest(ModelState);
        //    }
        //}

        [HttpPost]
        public async Task<IHttpActionResult> Dislike()
        {
            try
            {
                var comment = new Comment();
                var idCmt = Guid.Parse(HttpContext.Current.Request["Id"]);
                comment = db.Comments.Find(idCmt);
                if (comment == null)
                {
                    ModelState.AddModelError("Comment", "Comment doesn't exist");
                    return BadRequest(ModelState);
                }
                var userId = User.Identity.GetUserId();
                var cmtLikeCheck = db.CommentLikes.SingleOrDefault(x => x.UserID == userId && x.Id == idCmt);
                var cmtLike = new CommentLike();
                int totalLike, totalDislike;

                if (cmtLikeCheck == null)
                {
                    cmtLike.Id = idCmt;
                    cmtLike.UserID = User.Identity.GetUserId();
                    cmtLike.TimeLike = DateTime.Now;
                    //cmtLike.IsLiked = false;

                    db.CommentLikes.Add(cmtLike);
                    db.SaveChanges();
                }
                else
                {
                    if (cmtLikeCheck.IsLiked)
                    {
                        cmtLikeCheck.TimeLike = DateTime.Now;
                        cmtLikeCheck.IsLiked = false;
                        db.Entry(cmtLikeCheck).State = EntityState.Modified;
                    }
                    else
                    {
                        db.CommentLikes.Remove(cmtLikeCheck);
                    }
                    db.SaveChanges();
                }

                totalLike = db.CommentLikes.Where(x => x.IsLiked == true
                                                    && x.UserID == userId
                                                    && x.Id == idCmt).Count();
                totalDislike = db.CommentLikes.Where(x => x.IsLiked == false
                                                       && x.UserID == userId
                                                       && x.Id == idCmt).Count();
                comment.Like = totalLike;
                comment.Dislike = totalDislike;
                db.SaveChanges();
                return Json(comment);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
