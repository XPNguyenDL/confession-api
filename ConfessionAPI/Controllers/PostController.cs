using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using ConfessionAPI.Areas.User.Data;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Newtonsoft.Json;

namespace ConfessionAPI.Controllers
{
    //[Authorize]
    public class PostController : ApiController
    {
        private ConfessionDbContext db = new ConfessionDbContext();

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
                    post.Avatar = "";
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

        [HttpGet]
        public IHttpActionResult FindPost()
        {
            try
            {
                string keyword = HttpContext.Current.Request["keyword"];
                var listKey = keyword.Split(' ');
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                foreach (var key in listKey)
                {
                    if (posts.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            posts = posts.Where(x =>
                                x.Title.ToLower().Contains(key) ||
                                x.Content.ToLower().Contains(key)).ToList();
                        }
                    }
                    else
                    {
                        break;
                    }

                }
                posts = FilterPosts(posts);

                return Json(posts);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult FindPostCategory()
        {
            try
            {
                Guid categoryId = Guid.Parse(HttpContext.Current.Request["Id"]);
                var posts = db.Posts.ToList();
                var postResult = new List<Post>();
                Account account = new Account();

                foreach (var post in posts)
                {
                    var postHistorys = db.PostHistories.Where(x => x.PostId == post.Id).ToList();
                    foreach (var postHistory in postHistorys)
                    {
                        account = db.IdentityUsers.Find(postHistory.AccountId);
                        break;
                    }

                    if (account.UserProfile.NickName != null)
                    {
                        post.NickName = account.UserProfile.NickName;
                    }
                    else
                    {
                        post.NickName = "User@" + account.UserProfile.Id.Split('-')[0];
                    }

                    post.Avatar = account.UserProfile.Avatar;

                    foreach (var category in post.Categories)
                    {
                        if (category.Id == categoryId)
                        {
                            postResult.Add(post);
                            break;
                        }
                    }
                }
                posts = FilterPosts(posts);
                return Json(postResult);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
