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

        [HttpGet]
        public IHttpActionResult Index()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                //posts = posts.Where(s => s.Active == true).ToList();

                Account account = new Account();
                PostHistory history = new PostHistory();
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

                    if (post.Avatar != null)
                    {
                        post.Avatar = account.UserProfile.Avatar;

                    }
                    else
                    {
                        post.Avatar = "";
                    }

                }
                //posts.Where(x => (x.PostHistories = new List<PostHistory>()).Count() == 0).ToList();
                posts = posts.OrderByDescending(x => x.CreatedTime).ToList();

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

                posts = posts.OrderBy(x => x.CreatedTime).ToList();
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


                postResult = postResult.OrderByDescending(x => x.CreatedTime).ToList();

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
