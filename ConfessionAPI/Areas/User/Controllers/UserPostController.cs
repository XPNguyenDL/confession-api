using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using ConfessionAPI.Areas.User.Data;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserPostController : UserController
    {
        [HttpPost]
        public IHttpActionResult PostLike()
        {
            try
            {
                var postId = Guid.Parse(HttpContext.Current.Request["Id"]);
                var postLikes = db.PostLikes.Where(s => s.UserID == User.Identity.GetUserId()
                                            && s.Id == postId).ToList();
                return Json(postLikes);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create()
        {
            try
            {
                Post result = new Post();
                var data = HttpContext.Current.Request["Post"];
                PostCreateViewModel createModel = JsonConvert.DeserializeObject<PostCreateViewModel>(data);

                #region Create

                if (createModel.SelectedCategories == null || !createModel.SelectedCategories.Any())
                {
                    ModelState.AddModelError("SelectedCategories", "Phải chọn ít nhất một danh mục");
                    return BadRequest(ModelState);
                }
                if (ModelState.IsValid)
                {
                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        Title = createModel.Title,
                        Content = createModel.Content,
                        Like = 0,
                        Dislike = 0,
                        Report = 0,
                        Active = true,
                        Status = PostStatus.Valid,
                        CreatedTime = DateTime.Now,
                        PrivateMode = createModel.PrivateMode,
                        Categories = new List<Category>(),
                    };

                    // Add caterories
                    UpdatePostCategories(post, createModel.SelectedCategories);
                    result = post;
                    db.Posts.Add(post);

                    // Create PostHistory
                    var history = new PostHistory()
                    {
                        Id = Guid.NewGuid(),
                        AccountId = User.Identity.GetUserId(),
                        ActionTime = DateTime.Now,
                        PostId = post.Id,
                        HistoryAction = PostHistoryAction.Create,
                    };

                    db.PostHistories.Add(history);

                    #region Picture

                    List<string> listPath = new List<string>();
                    var ctx = HttpContext.Current;
                    var root = ctx.Server.MapPath("~/Uploads/Pictures/Post/" + post.Id);

                    if (!Directory.Exists(root))
                    {
                        Directory.CreateDirectory(root);
                    }

                    var provider = new MultipartFormDataStreamProvider(root);
                    await Request.Content.ReadAsMultipartAsync(provider)
                        .ContinueWith(async (a) =>
                        {
                            foreach (var file in provider.FileData)
                            {
                                string name = file.Headers.ContentDisposition.FileName;
                                name = Guid.NewGuid() + "_" + name.Trim('"');
                                var localFileName = file.LocalFileName;
                                var filePath = Path.Combine(root, name);
                                listPath.Add(post.Id + "/" + name);
                                File.Move(localFileName, filePath);
                            }


                        }).Unwrap();
                    foreach (var path in listPath)
                    {
                        var picture = new Picture()
                        {
                            Id = Guid.NewGuid(),
                            Path = path,
                            Active = true,
                            PostId = post.Id
                        };
                        db.Pictures.Add(picture);
                    }
                    db.SaveChanges();
                    #endregion

                }
                #endregion
                return Json(result);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Edit()
        {
            try
            {
                Post post = new Post();
                var temp = HttpContext.Current.Request["Post"];
                var totalFiles = HttpContext.Current.Request.Files.Count;
                PostCreateViewModel createModel = JsonConvert.DeserializeObject<PostCreateViewModel>(temp);
                var userId = User.Identity.GetUserId();

                //var postHistory = db.PostHistories.SingleOrDefault(x => x.AccountId == userId && x.PostId == createModel.Id);
                var postHistory = db.PostHistories.Where(x => x.AccountId == userId && x.PostId == createModel.Id).Take(1).SingleOrDefault();
                if (postHistory == null)
                {
                    ModelState.AddModelError("postHistory", "Error account update!");
                    return BadRequest(ModelState);
                }
                if (postHistory != null)
                {
                    post = db.Posts.Find(postHistory.PostId);
                    if (post == null)
                    {
                        ModelState.AddModelError("Post", "Post doesn't exist");
                        return BadRequest(ModelState);
                    }
                    post.Content = createModel.Content;
                    post.Title = createModel.Title;
                    post.PrivateMode = createModel.PrivateMode;
                    UpdatePostCategories(post, createModel.SelectedCategories);

                    db.Entry(post).State = EntityState.Modified;

                    postHistory.ActionTime = DateTime.Now;
                    postHistory.HistoryAction = PostHistoryAction.UpdateFull;

                    db.Entry(postHistory).State = EntityState.Modified;

                    // còn bug

                    var ctx = HttpContext.Current;
                    var root = ctx.Server.MapPath("~/Uploads/Pictures/Post/" + post.Id);

                    if (totalFiles > 0)
                    {
                        if (Directory.Exists(root))
                        {
                            var pictures = db.Pictures.Where(x => x.PostId == post.Id);
                            foreach (var item in pictures)
                            {
                                db.Pictures.Remove(item);
                            }
                            Directory.Delete(root, true);
                        }
                        #region Picture

                        List<string> listPath = new List<string>();


                        if (!Directory.Exists(root))
                        {
                            Directory.CreateDirectory(root);
                        }
                        var provider = new MultipartFormDataStreamProvider(root);
                        await Request.Content.ReadAsMultipartAsync(provider)
                            .ContinueWith(async (a) =>
                            {
                                foreach (var file in provider.FileData)
                                {
                                    string name = file.Headers.ContentDisposition.FileName;
                                    name = Guid.NewGuid() + "_" + name.Trim('"');
                                    var localFileName = file.LocalFileName;
                                    var filePath = Path.Combine(root, name);
                                    listPath.Add(post.Id + "/" + name);
                                    File.Move(localFileName, filePath);
                                }
                            }).Unwrap();

                        foreach (var path in listPath)
                        {
                            var picture = new Picture()
                            {
                                Id = Guid.NewGuid(),
                                Path = path,
                                Active = true,
                                PostId = post.Id
                            };
                            db.Pictures.Add(picture);
                        }
                        #endregion
                    }
                    db.SaveChanges();
                }

                return Json(post);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                ModelState.AddModelError("DB", "Error edit post or History is empty or Account error");
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult Delete()
        {
            try
            {
                Guid postId = Guid.Parse(HttpContext.Current.Request["Id"]);
                var userId = User.Identity.GetUserId();

                var listPostHistorys = db.PostHistories.Where(x => x.AccountId == userId && x.PostId == postId).ToList();
                if (listPostHistorys.Count == 0)
                {
                    ModelState.AddModelError("Error", "Bạn không thể xóa bài viết này");
                    return BadRequest(ModelState);
                }
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
                    ProcessDirectory(ctx.Server.MapPath("~/Uploads/Pictures/Post/"));
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

        [HttpPost]
        public async Task<IHttpActionResult> Like()
        {
            try
            {
                var post = new Post();
                var idPost = Guid.Parse(HttpContext.Current.Request["Id"]);

                post = db.Posts.FirstOrDefault(s => s.Id == idPost);

                if (post == null)
                {
                    ModelState.AddModelError("Post", "Post doesn't exist");
                    return BadRequest(ModelState);
                }

                var userId = User.Identity.GetUserId();
                var postLikeCheck = db.PostLikes.SingleOrDefault(x => x.UserID == userId && x.Id == idPost);
                var postLike = new PostLike();


                var userPost = GetUserByPost(post);
                var userLike = GetUserById(userId);
                var notify = new Notification();

                if (userPost.Id != userLike.Id)
                {
                    notify = new Notification()
                    {
                        Id = Guid.NewGuid(),
                        Avatar = userLike.UserProfile.Avatar,
                        IsRead = false,
                        NotifyName = userLike.UserProfile.NickName,
                        NotifyDate = DateTime.Now,
                        Description = $" đã thích bài viết của bạn!",
                        UserID = userPost.Id,
                        NotifyUserId = userLike.Id,
                        TypeNotify = TypeNotify.Like,
                        PostId = post.Id
                    };
                }

                if (postLikeCheck == null)
                {
                    postLike.Id = idPost;
                    postLike.UserID = User.Identity.GetUserId();
                    postLike.TimeLike = DateTime.Now;
                    postLike.IsLiked = true;

                    db.PostLikes.Add(postLike);
                    if (notify.UserID != null)
                    {
                        db.Notification.Add(notify);
                    }

                    db.SaveChanges();
                }
                else
                {
                    if (postLikeCheck.IsLiked == false)
                    {
                        postLikeCheck.TimeLike = DateTime.Now;
                        postLikeCheck.IsLiked = true;
                        db.Entry(postLikeCheck).State = EntityState.Modified;
                        if (notify.UserID != null)
                        {
                            db.Notification.Add(notify);
                        }
                    }
                    else
                    {
                        var oldNotifies = db.Notification.Where(s => s.PostId == post.Id &&
                                                                            s.NotifyUserId == userId &&
                                                                            s.TypeNotify == TypeNotify.Like).ToList();
                        foreach (var oldNotify in oldNotifies)
                        {
                            db.Notification.Remove(oldNotify);
                        }
                        db.PostLikes.Remove(postLikeCheck);
                    }
                    db.SaveChanges();
                }

                int totalLike, totalDislike;
                totalLike = db.PostLikes.Where(x => x.IsLiked == true
                                                    && x.Id == idPost).Count();
                totalDislike = db.PostLikes.Where(x => x.IsLiked == false
                                                       && x.Id == idPost).Count();
                post.Like = totalLike;
                post.Dislike = totalDislike;
                db.Entry(post).State = EntityState.Modified;
                db.SaveChanges();
                return Json(post);

            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
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

        [HttpPost]
        public async Task<IHttpActionResult> Dislike()
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
                var userId = User.Identity.GetUserId();
                var postLikeCheck = db.PostLikes.SingleOrDefault(x => x.UserID == userId && x.Id == idPost);
                var postLike = new PostLike();
                int totalLike, totalDislike;

                if (postLikeCheck == null)
                {
                    postLike.Id = idPost;
                    postLike.UserID = User.Identity.GetUserId();
                    postLike.TimeLike = DateTime.Now;
                    postLike.IsLiked = false;

                    db.PostLikes.Add(postLike);
                    db.SaveChanges();
                }
                else
                {
                    if (postLikeCheck.IsLiked)
                    {
                        postLikeCheck.TimeLike = DateTime.Now;
                        postLikeCheck.IsLiked = false;
                        db.Entry(postLikeCheck).State = EntityState.Modified;
                    }
                    else
                    {
                        db.PostLikes.Remove(postLikeCheck);
                    }
                    db.SaveChanges();
                }

                totalLike = db.PostLikes.Where(x => x.IsLiked == true
                                                    && x.Id == idPost).Count();
                totalDislike = db.PostLikes.Where(x => x.IsLiked == false
                                                       && x.Id == idPost).Count();
                post.Like = totalLike;
                post.Dislike = totalDislike;
                return Json(post);

            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult Report()
        {
            try
            {
                var postId = Guid.Parse(HttpContext.Current.Request["id"]);
                var userId = User.Identity.GetUserId();

                var data = HttpContext.Current.Request["Report"];
                var report = JsonConvert.DeserializeObject<PostReport>(data);

                var postReportCheck = db.PostReports.SingleOrDefault(x => x.UserID == userId && x.Id == postId);
                if (postReportCheck == null)
                {
                    var post = db.Posts.FirstOrDefault(s => s.Id == postId);

                    if (post == null)
                    {
                        ModelState.AddModelError("Error", "Bài viết không tồn tại");
                        return BadRequest(ModelState);
                    }

                    var userPost = GetUserByPost(post);
                    var userReport = GetUserById(userId);
                    if (userPost.Id != userReport.Id)
                    {
                        var postReport = new PostReport()
                        {
                            Id = post.Id,
                            UserID = userId,
                            Description = report.Description,
                            TimeReport = DateTime.Now
                        };

                        db.PostReports.Add(postReport);
                        db.SaveChanges();
                    }
                    var listReports = db.PostReports.Where(s => s.Id == postId);
                    post.Report = listReports.Count();
                    if (listReports.Count() > 50)
                    {
                        post.Active = false;
                        post.Status = PostStatus.Violate;

                        var notify = new Notification()
                        {
                            Id = Guid.NewGuid(),
                            Avatar = "Default/Avatar_system.png",
                            IsRead = false,
                            NotifyName = "Hệ thống",
                            NotifyDate = DateTime.Now,
                            Description = $" đã ẩn bài viết của bạn do vi phạm quy tắc của cộng đồng!",
                            UserID = userPost.Id,
                            NotifyUserId = "",
                            TypeNotify = TypeNotify.Report,
                            PostId = post.Id
                        };
                        db.Notification.Add(notify);

                    }

                    db.Entry(post).State = EntityState.Modified;
                    db.SaveChanges();

                    post = db.Posts.SingleOrDefault(s => s.Id == postId);
                    return Json(post);

                }
                else
                {
                    ModelState.AddModelError("Error", "Bạn đã báo cáo bài viết này");
                    return BadRequest(ModelState);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Upload(Guid id)
        {
            #region Upload Image
            var posts = db.Posts.ToList().Select(s => new Post(s)).ToList();
            posts = posts.Where(x =>
                x.Id == id).ToList();
            var listPicture = new List<Picture>();
            if (posts.Count == 1)
            {
                List<string> listPath = new List<string>();
                var ctx = HttpContext.Current;
                var root = ctx.Server.MapPath("~/Uploads/Pictures/Post/" + id);
                var provider = new MultipartFormDataStreamProvider(root);
                try
                {
                    if (!Directory.Exists(root))
                    {
                        Directory.CreateDirectory(root);
                    }

                    await Request.Content.ReadAsMultipartAsync(provider)
                        .ContinueWith(async (a) =>
                        {
                            foreach (var file in provider.FileData)
                            {
                                string name = file.Headers.ContentDisposition.FileName;
                                name = Guid.NewGuid() + "_" + name.Trim('"');
                                var localFileName = file.LocalFileName;
                                var filePath = Path.Combine(root, name);
                                listPath.Add(id + "/" + name);
                                File.Move(localFileName, filePath);
                            }


                        }).Unwrap();


                    foreach (var path in listPath)
                    {
                        var picture = new Picture()
                        {
                            Id = Guid.NewGuid(),
                            Path = path,
                            Active = true,
                            PostId = id
                        };
                        db.Pictures.Add(picture);
                        listPicture.Add(picture);
                    }

                    db.SaveChanges();

                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Error", e.Message);
                    return BadRequest(ModelState);
                }
            }

            return Json(listPicture);

            #endregion

        }

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
        private Account GetUserByPost(Post post)
        {
            var user = new Account();

            var postHistorys = db.PostHistories.Where(x => x.PostId == post.Id).ToList();
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

        private void UpdatePostCategories(Post post, Guid[] selectedCategoties)
        {
            if (selectedCategoties == null) return;

            var categories = db.Categories.ToList();
            var currentCateIds = new HashSet<Guid>(post.Categories.Select(x => x.Id));

            foreach (var cate in categories)
            {
                if (selectedCategoties.ToList().Contains(cate.Id))
                {
                    if (!currentCateIds.ToList().Contains(cate.Id))
                    {
                        post.Categories.Add(cate);
                    }
                }
                else if (currentCateIds.ToList().Contains(cate.Id))
                {
                    post.Categories.Remove(cate);
                }
            }

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
                    post.Avatar = "";
                }
            }
            posts = posts.OrderByDescending(x => x.CreatedTime).ToList();
            return posts;
        }
    }
}
