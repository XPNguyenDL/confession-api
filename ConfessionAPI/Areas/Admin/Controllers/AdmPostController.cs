using System;
using System.Collections.Generic;
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

        [HttpGet]
        public IHttpActionResult Index()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                //posts = posts.Where(s => s.Active == true).ToList();
                posts = posts.OrderByDescending(x => x.CreatedTime).ToList();
                return Json(posts);
            }
            catch (Exception ex)
            {
                return Json(ex);
            }
        }

        [HttpGet]
        public IHttpActionResult PostViolate()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                posts = posts.Where(s => s.Active == false).ToList();
                posts = posts.OrderByDescending(x => x.CreatedTime).ToList();
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
                var newPost = db.Posts.ToList();
                return Json(newPost);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
