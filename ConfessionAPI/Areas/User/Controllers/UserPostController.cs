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
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserPostController : UserController
    {
        private ConfessionDbContext db = new ConfessionDbContext();

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
            Post result = new Post();
            try
            {
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
                        Status = PostStatus.Violate,
                        CreatedTime = DateTime.Now,
                        Categories = new List<Category>(),
                    };

                    // Add caterories
                    UpdatePostCategories(post, createModel.SelectedCategories);
                    db.Posts.Add(post);

                    // Create PostHistory
                    var history = new PostHistory()
                    {
                        Id = Guid.NewGuid(),
                        AccountId = User.Identity.GetUserId(),
                        ActionTime = DateTime.Now,
                        PostId = post.Id,
                        HistoryAction = PostHistoryAction.Create,
                        OriginalPost = null,
                        ModifiedPost = JsonConvert.SerializeObject(post)
                    };

                    db.PostHistories.Add(history);

                    #region Picture

                    List<string> listPath = new List<string>();
                    var ctx = HttpContext.Current;
                    var root = ctx.Server.MapPath("~/Uploads/Pictures/Post/" + post.Id);
                    var provider = new MultipartFormDataStreamProvider(root);

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
            Post post = new Post();
            try
            {
                var temp = HttpContext.Current.Request["Post"];
                PostCreateViewModel createModel = JsonConvert.DeserializeObject<PostCreateViewModel>(temp);
                var userId = User.Identity.GetUserId();

                var postHistory = db.PostHistories.SingleOrDefault(x => x.AccountId == userId && x.PostId == createModel.Id);
                if (postHistory == null)
                {
                    ModelState.AddModelError("postHistory", "Error account update!");
                    return BadRequest(ModelState);
                }
                if (postHistory != null)
                {
                    var oldPost = db.Posts.Find(postHistory.PostId);
                    post = db.Posts.Find(postHistory.PostId);
                    if (post == null)
                    {
                        ModelState.AddModelError("Post", "Post doesn't exist");
                        return BadRequest(ModelState);
                    }
                    post.Content = createModel.Content;
                    post.Title = createModel.Title;
                    UpdatePostCategories(post, createModel.SelectedCategories);

                    db.Entry(post).State = EntityState.Modified;

                    var history = new PostHistory()
                    {
                        Id = Guid.NewGuid(),
                        AccountId = User.Identity.GetUserId(),
                        ActionTime = DateTime.Now,
                        PostId = post.Id,
                        HistoryAction = PostHistoryAction.UpdateFull,
                        OriginalPost = JsonConvert.SerializeObject(oldPost),
                        ModifiedPost = JsonConvert.SerializeObject(post)
                    };

                    db.PostHistories.Add(history);

                    var ctx = HttpContext.Current;
                    var root = ctx.Server.MapPath("~/Uploads/Pictures/Post/" + post.Id);
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
                    var provider = new MultipartFormDataStreamProvider(root);
                    

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

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                ModelState.AddModelError("DB", "Error edit post or History is empty or Account error");
                return BadRequest(ModelState);
            }
            return Json(post);
        }

        [HttpPost]
        public IHttpActionResult Delete()
        {
            try
            {

                Guid postId = Guid.Parse(HttpContext.Current.Request["Id"]);
                var userId = User.Identity.GetUserId();
                
                var listPostHistorys = db.PostHistories.Where(x => x.AccountId == userId && x.PostId == postId).ToList();
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

        [HttpPost]
        public async Task<IHttpActionResult> Like()
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
                    postLike.IsLiked = true;

                    db.PostLikes.Add(postLike);
                    db.SaveChanges();
                }
                else
                {
                    if (postLikeCheck.IsLiked == false)
                    {
                        postLikeCheck.TimeLike = DateTime.Now;
                        postLikeCheck.IsLiked = true;
                        db.Entry(postLikeCheck).State = EntityState.Modified;
                    }
                    else
                    {
                        db.PostLikes.Remove(postLikeCheck);
                    }
                    db.SaveChanges();
                }

                totalLike = db.PostLikes.Where(x => x.IsLiked == true
                                                    && x.UserID == userId
                                                    && x.Id == idPost).Count();
                totalDislike = db.PostLikes.Where(x => x.IsLiked == false
                                                       && x.UserID == userId
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
                                                    && x.UserID == userId
                                                    && x.Id == idPost).Count();
                totalDislike = db.PostLikes.Where(x => x.IsLiked == false
                                                       && x.UserID == userId
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
    }
}
