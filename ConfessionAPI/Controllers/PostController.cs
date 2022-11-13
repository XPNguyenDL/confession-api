using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;

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
                post.PostReports.Clear();
            }

            posts = posts.Where(s => s.Active).ToList();
            posts = posts.OrderByDescending(x => x.CreatedTime).ToList();
            
            return posts;
        }

        [HttpGet]
        public IHttpActionResult Index()
        {
            try
            {
                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                posts = posts.Where(s => s.Active).ToList();
                posts = FilterPosts(posts);

                return Json(posts);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult PostByUser()
        {
            try
            {
                string userId = HttpContext.Current.Request["Id"];
                var user = db.IdentityUsers.FirstOrDefault(s => s.Id == userId);
                if (user == null)
                {
                    ModelState.AddModelError("Error", "Tài khoản không tồn tại");
                    return BadRequest(ModelState);
                }
                var posts = db.Posts.Where(s => s.PostHistories.Any(h => h.AccountId == userId)).ToList();
                return Json(FilterPosts(posts));

            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult FindPost()
        {
            try
            {
                string keyword = HttpContext.Current.Request["keyword"];
                keyword = keyword.Replace("\n", "").Replace("\r", "");
                keyword = RemoveSignVietnameseString(keyword).ToLower();
                var listKey = keyword.Split(' ');

                var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
                foreach (var key in listKey)
                {
                    if (posts.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            posts = posts.Where(x =>
                                RemoveSignVietnameseString(x.Title.ToLower()).Contains(key) ||
                                RemoveSignVietnameseString(x.Content.ToLower()).Contains(key) ||
                                x.Categories.Any(c => RemoveSignVietnameseString(c.Name).ToLower().Contains(key))).ToList();
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

                    if (post.PrivateMode)
                    {
                        post.Avatar = "Default/Avatar_default.png";
                    }
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

        [HttpGet]
        public IHttpActionResult HotPost()
        {
            var now = DateTime.Now.AddDays(-7);
            var hotPosts = db.Posts.ToList();
            foreach (var post in hotPosts)
            {
                post.Like = post.PostLikes.Where(s => s.IsLiked == true && s.TimeLike >= now).Count();
            }

            hotPosts = FilterPosts(hotPosts);
            hotPosts = hotPosts.OrderByDescending(x => x.Like).ToList();
            return Json(hotPosts.Take(10));
        }

        [HttpPost]
        public IHttpActionResult GetPostById()
        {
            try
            {
                var post = new Post();
                var idPost = Guid.Parse(HttpContext.Current.Request["Id"]);

                post = db.Posts.Find(idPost);

                if (post == null)
                {
                    ModelState.AddModelError("Post", "Post doesn't exist");
                    return BadRequest(ModelState);
                }

                Account account = new Account();

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
                    post.Avatar = "";
                }
                return Json(post);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }


        private readonly string[] VietnameseSigns = new string[]
        {

            "aAeEoOuUiIdDyY",

            "áàạảãâấầậẩẫăắằặẳẵ",

            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",

            "éèẹẻẽêếềệểễ",

            "ÉÈẸẺẼÊẾỀỆỂỄ",

            "óòọỏõôốồộổỗơớờợởỡ",

            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",

            "úùụủũưứừựửữ",

            "ÚÙỤỦŨƯỨỪỰỬỮ",

            "íìịỉĩ",

            "ÍÌỊỈĨ",

            "đ",

            "Đ",

            "ýỳỵỷỹ",

            "ÝỲỴỶỸ"
        };
        private string RemoveSignVietnameseString(string str)
        {
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return str;
        }
    }
}
